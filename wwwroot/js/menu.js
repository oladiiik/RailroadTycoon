document.getElementById('advToggle').addEventListener('click', () => {
    const adv = document.getElementById('advancedSettings');
    const current = getComputedStyle(adv).display;
    adv.style.display = (current === 'none') ? 'block' : 'none';
});

function showLoader() {
    if (document.getElementById('loaderOverlay')) return;

    const loader = document.createElement('div');
    loader.id = 'loaderOverlay';
    Object.assign(loader.style, {
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        background: 'rgba(0, 0, 0, 0.8)', 
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 10000
    });

    const text = document.createElement('div');
    Object.assign(text.style, {
        color: '#fff',
        fontSize: '1.5rem',
        fontFamily: 'sans-serif'
    });
    loader.appendChild(text);
    document.body.appendChild(loader);
    
    let dots = 0;
    const intervalId = setInterval(() => {
        dots = (dots % 3) + 1;
        text.textContent = 'Завантаження карти' + '.'.repeat(dots);
    }, 500);
    
    loader.dataset.intervalId = intervalId;
}

function hideLoader() {
    const loader = document.getElementById('loaderOverlay');
    if (!loader) return;
    clearInterval(+loader.dataset.intervalId);
    loader.remove();
}

document.getElementById('playButton').addEventListener('click', () => {
    let seedVal = document.getElementById('seedInput').value;
    if (!seedVal) {
        seedVal = Math.floor(Math.random() * 1_000_000).toString();
    }
    const params = new URLSearchParams({
        seed:           seedVal,
        freq:           document.getElementById('freqInput').value,
        octaves:        document.getElementById('octavesInput').value,
        lacunarity:     document.getElementById('lacunarityInput').value,
        gain:           document.getElementById('gainInput').value,
        cityCount:      document.getElementById('cityContInput').value,
        initialBudget:  document.getElementById('initialBudgetInput').value,
        costPerKm:      document.getElementById('costPerKmInput').value,
        rewardPerCity:  document.getElementById('rewardPerCityInput').value
    });
    
    showLoader();
    setTimeout(() => {
        window.location.href = '/map?' + params.toString();
    }, 50);
});

window.addEventListener('load', () => {
    hideLoader();
});
