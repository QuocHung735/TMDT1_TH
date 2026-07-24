(() => {
    const header = document.querySelector('.store-header');

    if (header) {
        let lastY = window.scrollY;
        window.addEventListener('scroll', () => {
            const currentY = window.scrollY;
            header.classList.toggle('is-scrolled', currentY > 8);
            header.classList.toggle(
                'is-hidden',
                currentY > lastY && currentY > 180);
            lastY = currentY;
        }, { passive: true });
    }

    const refreshCartCount = async () => {
        const counters = document.querySelectorAll('[data-cart-count]');
        if (counters.length === 0) return;

        try {
            const response = await fetch('/gio-hang/tom-tat', {
                headers: {
                    Accept: 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                cache: 'no-store'
            });

            if (!response.ok) return;
            const data = await response.json();
            const count = Number(data.itemCount || 0);

            counters.forEach(counter => {
                counter.textContent = String(count);
                counter.setAttribute(
                    'aria-label',
                    `${count} sản phẩm trong giỏ hàng`);
            });
        } catch {
            // Không chặn giao diện nếu endpoint giỏ hàng tạm thời không phản hồi.
        }
    };

    window.mayHomeCart = {
        refresh: refreshCartCount
    };

    refreshCartCount();
})();
