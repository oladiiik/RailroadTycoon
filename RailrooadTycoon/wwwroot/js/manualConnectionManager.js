export class ManualConnectionManager {
    constructor(cities, opts) {
        this.cities        = cities;
        this.costPerKm     = opts.costPerKm;
        this.budget        = opts.initialBudget;
        this.rewardPerCity = opts.rewardPerCity;

        this.connections   = new Set();
        this.reachable     = new Set();          

        this.api        = opts.apiBase  ?? '';
        this.seedParams = opts.seedParams;

        this.history = [];
    }
    
    async tryConnect(from, to) {
        const res = await fetch(`${this.api}/connect${this.seedParams}`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',          
            body: JSON.stringify({ from, to })
        });

        if (!res.ok) return { success:false, reason:'network' };

        const j = await res.json();              
        if (j.success) {
            this.budget = j.newBudget;
            this.connections.add(`${from}-${to}`);
            this.reachable.add(from);
            this.reachable.add(to);

            this.history.push({ from, to });
        }
        return j;
    }

    async undo() {
        if (this.history?.length === 0)
            return { success:false, reason:'empty' };

        const res = await fetch(`${this.api}/undo${this.seedParams}`, {
            method: 'POST',
            credentials: 'same-origin'
        });
        if (!res.ok) return { success:false, reason:'network' };

        const j = await res.json();
        if (j.success) {
            const last = this.history.pop();
            this.connections.delete(`${last.from}-${last.to}`);
            this.connections.delete(`${last.to}-${last.from}`);

            this.reachable.clear();
            for (const c of this.connections) {
                const [a, b] = c.split('-').map(Number);
                this.reachable.add(a);
                this.reachable.add(b);
            }
            this.budget = j.newBudget;
        }
        return j;
    }

    remainingBudget() { return this.budget; }
    
    getAffordableTargets(fromIndex) {
        if (this.reachable.size && !this.reachable.has(fromIndex)) return [];

        const list = [];
        const { x: ax, y: ay } = this.cities[fromIndex];

        for (let i = 0; i < this.cities.length; i++) {
            if (i === fromIndex) continue;
            
            if (this.connections.has(`${fromIndex}-${i}`) ||
                this.connections.has(`${i}-${fromIndex}`)) continue;
            
            const dx = this.cities[i].x - ax;
            const dy = this.cities[i].y - ay;
            const cost = Math.hypot(dx, dy) * this.costPerKm;

            const isReachable = this.reachable.has(i);
            
            if (!isReachable && cost <= this.budget)
                list.push({ index:i, cost, reachable:false });
        }
        return list;
    }
}
