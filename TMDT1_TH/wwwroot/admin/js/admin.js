(() => {
    const shell = document.querySelector('[data-app-shell]');
    const sidebarToggle = document.querySelector('[data-sidebar-toggle]');
    const sidebarOverlay = document.querySelector('[data-sidebar-overlay]');
    const closeSidebar = () => shell?.classList.remove('sidebar-open');
    sidebarToggle?.addEventListener('click', () => shell?.classList.toggle('sidebar-open'));
    sidebarOverlay?.addEventListener('click', closeSidebar);

    const dropdownToggle = document.querySelector('[data-dropdown-toggle]');
    const dropdownMenu = document.querySelector('[data-dropdown-menu]');
    dropdownToggle?.addEventListener('click', (event) => {
        event.stopPropagation();
        dropdownMenu?.classList.toggle('is-open');
    });
    document.addEventListener('click', () => dropdownMenu?.classList.remove('is-open'));

    document.querySelectorAll('[data-toast]').forEach((toast) => {
        const close = () => toast.classList.add('is-leaving');
        toast.querySelector('[data-toast-close]')?.addEventListener('click', close);
        window.setTimeout(close, 4500);
    });

    document.querySelectorAll('[data-modal-target]').forEach((trigger) => {
        trigger.addEventListener('click', () => {
            const id = trigger.getAttribute('data-modal-target');
            const modal = id ? document.getElementById(id) : null;
            modal?.classList.add('is-open');
            modal?.setAttribute('aria-hidden', 'false');
            document.body.classList.add('modal-open');
        });
    });
    document.querySelectorAll('[data-modal-close]').forEach((trigger) => {
        trigger.addEventListener('click', () => {
            const modal = trigger.closest('.modal');
            modal?.classList.remove('is-open');
            modal?.setAttribute('aria-hidden', 'true');
            document.body.classList.remove('modal-open');
        });
    });
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            document.querySelectorAll('.modal.is-open').forEach((modal) => {
                modal.classList.remove('is-open');
                modal.setAttribute('aria-hidden', 'true');
            });
            document.body.classList.remove('modal-open');
            closeSidebar();
        }
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            document.querySelector('.global-search input')?.focus();
        }
    });

    document.querySelectorAll('[data-table-search]').forEach((input) => {
        input.addEventListener('input', () => {
            const target = input.getAttribute('data-table-search');
            const table = target ? document.getElementById(target) : null;
            const query = input.value.trim().toLocaleLowerCase('vi');
            table?.querySelectorAll('[data-search-row]').forEach((row) => {
                row.hidden = !row.textContent.toLocaleLowerCase('vi').includes(query);
            });
        });
    });

    document.querySelectorAll('.card, .panel').forEach((scope) => {
        const checkAll = scope.querySelector('[data-check-all]');
        const checks = [...scope.querySelectorAll('[data-row-check]')];
        const bulkBar = scope.querySelector('[data-bulk-bar]');
        const count = scope.querySelector('[data-selected-count]');
        const refresh = () => {
            const selected = checks.filter((check) => check.checked).length;
            if (count) count.textContent = selected;
            bulkBar?.classList.toggle('is-visible', selected > 0);
            if (checkAll) {
                checkAll.checked = checks.length > 0 && selected === checks.length;
                checkAll.indeterminate = selected > 0 && selected < checks.length;
            }
        };
        checkAll?.addEventListener('change', () => { checks.forEach((check) => check.checked = checkAll.checked); refresh(); });
        checks.forEach((check) => check.addEventListener('change', refresh));
    });

    document.querySelectorAll('[data-tabs]').forEach((tabs) => {
        const buttons = [...tabs.querySelectorAll('[data-tab]')];
        buttons.forEach((button) => button.addEventListener('click', () => {
            const target = button.getAttribute('data-tab');
            buttons.forEach((item) => item.classList.toggle('is-active', item === button));
            document.querySelectorAll('[data-tab-panel]').forEach((panel) => panel.classList.toggle('is-hidden', panel.getAttribute('data-tab-panel') !== target));
        }));
    });

    const moneyInputs = document.querySelectorAll('[data-money-input]');
    moneyInputs.forEach((input) => {
        input.addEventListener('input', () => input.value = input.value.replace(/\D/g, ''));
        input.addEventListener('blur', () => {
            const value = Number(input.value.replace(/\D/g, ''));
            if (!Number.isNaN(value)) input.value = value.toLocaleString('vi-VN');
        });
        input.dispatchEvent(new Event('blur'));
    });

    document.querySelectorAll('input[name="duration"]').forEach((radio) => {
        radio.addEventListener('change', () => {
            const range = document.querySelector('[data-date-range]');
            range?.classList.toggle('is-disabled', radio.checked && radio.value === 'forever');
            range?.querySelectorAll('input').forEach((input) => input.disabled = radio.checked && radio.value === 'forever');
        });
    });

    const variantBody = document.querySelector('[data-variant-body]');
    const generateVariants = () => {
        if (!variantBody) return;
        const colors = (document.getElementById('variant-colors')?.value || '').split(',').map(x => x.trim()).filter(Boolean);
        const sizes = (document.getElementById('variant-sizes')?.value || '').split(',').map(x => x.trim()).filter(Boolean);
        const variants = [];
        colors.forEach((color) => sizes.forEach((size) => variants.push({ color, size })));
        variantBody.innerHTML = variants.map((variant, index) => `
            <tr>
                <td><div class="variant-name"><span style="--swatch:${['#f8fafc','#b9a7ff','#8be0c7'][index % 3]}"></span><strong>${variant.color} / ${variant.size}</strong></div></td>
                <td><input class="table-input" value="SHIRT-${slug(variant.color)}-${variant.size.toUpperCase()}" /></td>
                <td><input class="table-input table-input--small" type="number" value="${10 + index * 3}" min="0" /></td>
                <td><label class="mini-switch"><input type="checkbox" checked /><i></i></label></td>
            </tr>`).join('');
        const count = document.querySelector('[data-variant-count]');
        if (count) count.textContent = `${variants.length} biến thể`;
    };
    const slug = (text) => text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/[^a-zA-Z0-9]/g, '').toUpperCase().slice(0, 4);
    document.querySelector('[data-generate-variants]')?.addEventListener('click', generateVariants);
    generateVariants();

    const stepLinks = [...document.querySelectorAll('.editor-step')];
    if (stepLinks.length) {
        const sections = stepLinks.map(link => document.querySelector(link.getAttribute('href'))).filter(Boolean);
        const observer = new IntersectionObserver((entries) => {
            const visible = entries.filter(x => x.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
            if (!visible) return;
            stepLinks.forEach(link => link.classList.toggle('is-active', link.getAttribute('href') === `#${visible.target.id}`));
        }, { rootMargin: '-20% 0px -60% 0px', threshold: [0.1, 0.4] });
        sections.forEach(section => observer.observe(section));
    }
})();
