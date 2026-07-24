(() => {
    const header = document.querySelector('.store-header');
    if (!header) return;

    let lastY = window.scrollY;
    window.addEventListener('scroll', () => {
        const currentY = window.scrollY;
        header.classList.toggle('is-scrolled', currentY > 8);
        header.classList.toggle(
            'is-hidden',
            currentY > lastY && currentY > 180);
        lastY = currentY;
    }, { passive: true });
})();
