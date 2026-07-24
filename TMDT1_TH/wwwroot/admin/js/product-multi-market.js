(() => {
    const form =
        document.querySelector(
            '[data-product-form]');

    const marketSelect =
        document.querySelector(
            '[data-product-market]');

    if (!form || !marketSelect) return;

    const selectedFromServer =
        new Set(
            String(
                marketSelect.dataset
                    .selectedMarkets || '')
                .split(',')
                .map(value =>
                    Number(value.trim()))
                .filter(value =>
                    Number.isInteger(value) &&
                    value > 0)
        );

    if (selectedFromServer.size === 0 &&
        marketSelect.value) {
        selectedFromServer.add(
            Number(marketSelect.value));
    }

    const wrapper =
        document.createElement('section');

    wrapper.className =
        'multi-market-pricing';

    wrapper.innerHTML = `
        <div class="multi-market-pricing__header">
            <div>
                <strong>Áp dụng giá cho thị trường</strong>
                <small>
                    Chọn một hoặc nhiều thị trường. Tất cả thị trường
                    được chọn sẽ nhận cùng bộ giá và thời gian áp dụng.
                </small>
            </div>
            <button type="button"
                    class="multi-market-pricing__all"
                    data-market-select-all>
                Chọn tất cả
            </button>
        </div>
        <div class="multi-market-pricing__options"
             data-market-options></div>
        <p class="multi-market-pricing__note">
            Thị trường trong ô phía trên là nguồn giá đang hiển thị.
            Khi đổi nguồn, hệ thống sẽ nạp giá hiện có của thị trường đó.
        </p>`;

    const optionsContainer =
        wrapper.querySelector(
            '[data-market-options]');

    [...marketSelect.options]
        .filter(option =>
            option.value)
        .forEach(option => {
            const id =
                Number(option.value);

            const label =
                document.createElement(
                    'label');

            label.className =
                'multi-market-option';

            const input =
                document.createElement(
                    'input');

            input.type = 'checkbox';
            input.name = 'MarketIds';
            input.value = option.value;
            input.checked =
                selectedFromServer.has(id);

            const text =
                document.createElement(
                    'span');

            text.textContent =
                option.textContent?.trim()
                || option.value;

            label.append(
                input,
                text);

            optionsContainer.appendChild(
                label);
        });

    marketSelect
        .closest('.form-field')
        ?.after(wrapper);

    const style =
        document.createElement('style');

    style.textContent = `
        .multi-market-pricing {
            padding: 15px;
            background: #faf9ff;
            border: 1px solid #e4e0f2;
            border-radius: 14px;
        }

        .multi-market-pricing__header {
            display: flex;
            align-items: flex-start;
            justify-content: space-between;
            gap: 14px;
        }

        .multi-market-pricing__header strong,
        .multi-market-pricing__header small {
            display: block;
        }

        .multi-market-pricing__header strong {
            color: #17132d;
            font-size: 11px;
        }

        .multi-market-pricing__header small,
        .multi-market-pricing__note {
            margin-top: 4px;
            color: #77718c;
            font-size: 9px;
            line-height: 1.5;
        }

        .multi-market-pricing__options {
            display: grid;
            grid-template-columns:
                repeat(auto-fit, minmax(190px, 1fr));
            gap: 9px;
            margin-top: 13px;
        }

        .multi-market-option {
            display: flex;
            align-items: center;
            min-height: 42px;
            padding: 9px 11px;
            gap: 9px;
            cursor: pointer;
            background: #fff;
            border: 1px solid #ddd8ec;
            border-radius: 11px;
            font-size: 10px;
            font-weight: 600;
        }

        .multi-market-option:has(input:checked) {
            color: #5d4ed4;
            background: #f0edff;
            border-color: #9b8df5;
        }

        .multi-market-option input {
            width: 16px;
            height: 16px;
            accent-color: #7c69ee;
        }

        .multi-market-pricing__all {
            min-height: 32px;
            padding: 0 10px;
            color: #6654d9;
            cursor: pointer;
            background: #fff;
            border: 1px solid #d8d1ff;
            border-radius: 9px;
            font-size: 9px;
            font-weight: 700;
            white-space: nowrap;
        }

        .multi-market-pricing__note {
            margin-bottom: 0;
        }
    `;

    document.head.appendChild(style);

    const checkboxes = () =>
        [...wrapper.querySelectorAll(
            'input[name="MarketIds"]')];

    const checkSourceMarket = () => {
        if (!marketSelect.value) return;

        const source =
            wrapper.querySelector(
                `input[name="MarketIds"][value="${CSS.escape(
                    marketSelect.value)}"]`);

        if (source) source.checked = true;
    };

    marketSelect.addEventListener(
        'change',
        () => {
            checkSourceMarket();
        });

    wrapper.addEventListener(
        'change',
        event => {
            const checkbox =
                event.target.closest(
                    'input[name="MarketIds"]');

            if (!checkbox) return;

            if (checkbox.value ===
                    marketSelect.value &&
                !checkbox.checked) {
                checkbox.checked = true;
            }
        });

    wrapper
        .querySelector(
            '[data-market-select-all]')
        ?.addEventListener(
            'click',
            event => {
                const items =
                    checkboxes();

                const allSelected =
                    items.length > 0 &&
                    items.every(item =>
                        item.checked);

                items.forEach(item => {
                    item.checked =
                        !allSelected;
                });

                checkSourceMarket();

                event.currentTarget
                    .textContent =
                    allSelected
                        ? 'Chọn tất cả'
                        : 'Bỏ chọn khác';
            });

    form.addEventListener(
        'submit',
        event => {
            checkSourceMarket();

            const selected =
                checkboxes()
                    .filter(item =>
                        item.checked);

            if (marketSelect.value &&
                selected.length === 0) {
                event.preventDefault();

                alert(
                    'Vui lòng chọn ít nhất một thị trường áp dụng giá.');
            }
        },
        true);

    checkSourceMarket();
})();
