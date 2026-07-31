(() => {
    const form = document.querySelector('[data-product-form]');
    if (!form) return;

    const navigation = document.querySelector(
        '[data-product-editor-navigation], .editor-steps');

    const sections = [
        document.getElementById('general'),
        document.getElementById('media'),
        document.getElementById('variants'),
        document.getElementById('logistics'),
        document.getElementById('compliance'),
        document.getElementById('publish')
    ].filter(Boolean);

    const hasVariants = document.querySelector(
        '[data-product-has-variants]');

    const removeField = (id) => {
        const input = document.getElementById(id);
        if (!input) return false;

        const label = input.closest('label');
        if (label) {
            label.remove();
            return true;
        }

        input.disabled = true;
        input.hidden = true;
        return true;
    };

    const simplifyDescription = () => {
        removeField('ShortDescription');

        const description =
            document.getElementById('Description');

        const label = description?.closest('label');
        if (!label) return;

        const title = label.querySelector(':scope > span');
        if (title) {
            title.innerHTML =
                'Mô tả <em>* khi đăng bán</em>';
        }

        label.querySelectorAll('small').forEach((hint) => {
            if (/110|ký tự/i.test(hint.textContent || '')) {
                hint.remove();
            }
        });
    };

    const removePackageFields = () => {
        const ids = [
            'Weight',
            'PackageLengthCm',
            'PackageWidthCm',
            'PackageHeightCm'
        ];

        const containers = new Set();

        ids.forEach((id) => {
            const input = document.getElementById(id);
            const grid = input?.closest('.form-grid');

            if (grid) containers.add(grid);
            removeField(id);
        });

        containers.forEach((container) => {
            if (!container.querySelector(
                    'input, select, textarea')) {
                container.remove();
            }
        });
    };

    const simplifyReadiness = () => {
        const container = document.querySelector(
            '[data-readiness-checks]');

        if (!container) return;

        container.querySelectorAll('span').forEach((item) => {
            const text = item.textContent || '';

            if (/110\s*ký tự/i.test(text) ||
                /khối lượng và kích thước/i.test(text)) {
                item.remove();
                return;
            }

            if (/mô tả chi tiết/i.test(text)) {
                const icon = item.querySelector('i');

                item.textContent = 'Có mô tả sản phẩm';
                if (icon) item.prepend(icon);
            }
        });
    };

    const links = navigation
        ? [...navigation.querySelectorAll(
            'a[href^="#"]')]
        : [];

    let activeIndex = 0;

    const updateVariantLabels = () => {
        const enabled = hasVariants?.checked ?? false;

        const step = links.find((link) =>
            link.getAttribute('href') === '#variants');

        const stepTitle = step?.querySelector('strong');
        const stepHint = step?.querySelector('small');

        if (stepTitle) {
            stepTitle.textContent = enabled
                ? 'Biến thể & tồn kho'
                : 'Giá & tồn kho';
        }

        if (stepHint) {
            stepHint.textContent = enabled
                ? 'Quản lý từng SKU riêng'
                : 'Giá và kho sản phẩm đơn';
        }

        const section = document.getElementById('variants');
        const heading =
            section?.querySelector(
                '.form-section__header h2');

        const description =
            section?.querySelector(
                '.form-section__header p');

        if (heading) {
            heading.textContent = enabled
                ? 'Biến thể và tồn kho'
                : 'Giá và tồn kho';
        }

        if (description) {
            description.textContent = enabled
                ? 'Tạo tổ hợp phân loại, sau đó chỉnh giá và tồn kho trên từng thẻ biến thể.'
                : 'Nhập giá bán và tồn kho cho sản phẩm không có phân loại.';
        }
    };

    const ensureStepFooter = () => {
        let footer = form.querySelector(
            '[data-product-step-footer]');

        if (footer) return footer;

        footer = document.createElement('div');
        footer.className =
            'product-step-footer card';
        footer.dataset.productStepFooter = '';

        footer.innerHTML = `
            <div class="product-step-footer__progress">
                <small data-product-step-label></small>
                <strong data-product-step-title></strong>
            </div>
            <div class="product-step-footer__actions">
                <button class="btn btn-light"
                        type="button"
                        data-product-step-previous>
                    <i class="bi bi-arrow-left"></i>
                    Quay lại
                </button>
                <button class="btn btn-primary"
                        type="button"
                        data-product-step-next>
                    Tiếp tục
                    <i class="bi bi-arrow-right"></i>
                </button>
            </div>`;

        const actions =
            form.querySelector('.product-form-actions');

        if (actions) {
            form.insertBefore(footer, actions);
        } else {
            form.appendChild(footer);
        }

        return footer;
    };

    const footer = ensureStepFooter();
    const previousButton = footer.querySelector(
        '[data-product-step-previous]');
    const nextButton = footer.querySelector(
        '[data-product-step-next]');
    const stepLabel = footer.querySelector(
        '[data-product-step-label]');
    const stepTitle = footer.querySelector(
        '[data-product-step-title]');
    const finalActions =
        form.querySelector('.product-form-actions');

    const getStepTitle = (index) => {
        const link = links[index];
        return link?.querySelector('strong')
            ?.textContent?.trim() ||
            sections[index]?.querySelector('h2')
                ?.textContent?.trim() ||
            `Bước ${index + 1}`;
    };

    const activate = (
        index,
        { focus = false, updateHash = true } = {}) => {
        if (!sections.length) return;

        activeIndex = Math.max(
            0,
            Math.min(index, sections.length - 1));

        sections.forEach((section, sectionIndex) => {
            section.hidden =
                sectionIndex !== activeIndex;
        });

        links.forEach((link, linkIndex) => {
            const active = linkIndex === activeIndex;

            link.classList.toggle(
                'is-active',
                active);

            if (active) {
                link.setAttribute(
                    'aria-current',
                    'step');
            } else {
                link.removeAttribute(
                    'aria-current');
            }
        });

        if (stepLabel) {
            stepLabel.textContent =
                `Bước ${activeIndex + 1} / ${sections.length}`;
        }

        if (stepTitle) {
            stepTitle.textContent =
                getStepTitle(activeIndex);
        }

        if (previousButton) {
            previousButton.hidden =
                activeIndex == 0;
        }

        if (nextButton) {
            nextButton.hidden =
                activeIndex == sections.length - 1;
        }

        if (finalActions) {
            finalActions.hidden =
                activeIndex != sections.length - 1;
        }

        if (updateHash) {
            const id = sections[activeIndex]?.id;
            if (id) {
                history.replaceState(
                    null,
                    '',
                    `#${id}`);
            }
        }

        if (focus) {
            sections[activeIndex]
                ?.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
        }
    };

    links.forEach((link, index) => {
        link.addEventListener('click', (event) => {
            event.preventDefault();
            activate(index, { focus: true });
        });
    });

    previousButton?.addEventListener('click', () => {
        activate(activeIndex - 1, { focus: true });
    });

    nextButton?.addEventListener('click', () => {
        activate(activeIndex + 1, { focus: true });
    });

    form.addEventListener(
        'invalid',
        (event) => {
            const details =
                event.target.closest(
                    '[data-variant-details]');

            if (details?.hidden) {
                details.hidden = false;

                const row =
                    details.closest(
                        '[data-variant-row]');

                const button =
                    row?.querySelector(
                        '[data-toggle-variant-details]');

                if (button) {
                    button.setAttribute(
                        'aria-expanded',
                        'true');

                    button.innerHTML =
                        '<i class="bi bi-chevron-up"></i> Thu gọn';
                }
            }

            const section =
                event.target.closest('.form-section');

            const index =
                sections.indexOf(section);

            if (index >= 0 && index !== activeIndex) {
                activate(index, {
                    focus: true
                });
            }
        },
        true);

    hasVariants?.addEventListener('change', () => {
        updateVariantLabels();
    });

    const initialSection = (() => {
        const invalid =
            form.querySelector(
                '.input-validation-error, ' +
                '.field-validation-error');

        const invalidSection =
            invalid?.closest('.form-section');

        if (invalidSection) return invalidSection;

        const hash =
            window.location.hash?.slice(1);

        return sections.find(
            (section) => section.id === hash);
    })();

    simplifyDescription();
    removePackageFields();
    updateVariantLabels();
    simplifyReadiness();

    const initialIndex =
        initialSection
            ? sections.indexOf(initialSection)
            : 0;

    activate(
        initialIndex >= 0 ? initialIndex : 0,
        { updateHash: false });

    const observer = new MutationObserver(() => {
        simplifyReadiness();
    });

    observer.observe(form, {
        childList: true,
        subtree: true
    });
})();