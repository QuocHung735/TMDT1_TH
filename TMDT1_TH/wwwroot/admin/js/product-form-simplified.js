(() => {
    const form = document.querySelector('[data-product-form]');
    if (!form) return;

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

        if (!description) return;

        const label = description.closest('label');
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

    const simplifyHeadings = () => {
        document
            .querySelectorAll(
                '.editor-step strong, .form-section__header h2')
            .forEach((element) => {
                const text =
                    (element.textContent || '').trim();

                if (text === 'Vận chuyển & giá') {
                    element.textContent = 'Giá bán';
                }

                if (text === 'Vận chuyển và lịch giá') {
                    element.textContent =
                        'Giá bán và lịch giá';
                }
            });

        document
            .querySelectorAll('.form-section__header p')
            .forEach((element) => {
                const text =
                    element.textContent || '';

                if (text.includes(
                        'Thông tin đóng gói được dùng')) {
                    element.textContent =
                        'Thiết lập thị trường, thời gian áp dụng và giá bán sản phẩm.';
                }
            });
    };

    const removeVariantWeightColumn = () => {
        const body =
            document.querySelector(
                '[data-product-variant-body]');

        if (!body) return;

        body
            .querySelectorAll(
                '[data-field="weight"]')
            .forEach((input) => {
                const cell = input.closest('td');
                cell?.remove();
            });

        const table = body.closest('table');
        const headers = table
            ? [...table.querySelectorAll('thead th')]
            : [];

        headers.forEach((header) => {
            if (/khối lượng|trọng lượng/i.test(
                    header.textContent || '')) {
                header.remove();
            }
        });
    };

    const simplifyReadiness = () => {
        const container =
            document.querySelector(
                '[data-readiness-checks]');

        if (!container) return;

        container
            .querySelectorAll('span')
            .forEach((item) => {
                const text =
                    item.textContent || '';

                if (/110\s*ký tự/i.test(text) ||
                    /khối lượng và kích thước/i.test(text)) {
                    item.remove();
                    return;
                }

                if (/mô tả chi tiết/i.test(text)) {
                    const icon =
                        item.querySelector('i');

                    item.textContent =
                        'Có mô tả sản phẩm';

                    if (icon) item.prepend(icon);
                }
            });
    };

    const apply = () => {
        simplifyDescription();
        removePackageFields();
        simplifyHeadings();
        removeVariantWeightColumn();
        simplifyReadiness();
    };

    apply();

    const observer = new MutationObserver(() => {
        removeVariantWeightColumn();
        simplifyReadiness();
    });

    observer.observe(form, {
        childList: true,
        subtree: true
    });
})();
