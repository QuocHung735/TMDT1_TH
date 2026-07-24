(() => {
    const root = document.querySelector('[data-product-details]');
    if (!root) return;

    const currency = root.dataset.currency || 'VND';
    const hasVariants = root.dataset.hasVariants === 'true';
    const variantScript = document.getElementById('store-variant-data');
    const variants = variantScript
        ? JSON.parse(variantScript.textContent || '[]')
        : [];

    const salePrice = root.querySelector('[data-sale-price]');
    const listPrice = root.querySelector('[data-list-price]');
    const sku = root.querySelector('[data-product-sku]');
    const stock = root.querySelector('[data-product-stock]');
    const mainImage = root.querySelector('[data-main-product-image]');
    const quantityInput = root.querySelector('[data-quantity-input]');
    const minusButton = root.querySelector('[data-quantity-minus]');
    const plusButton = root.querySelector('[data-quantity-plus]');
    const optionGroups = [...root.querySelectorAll('[data-option-id]')];
    const selectedValues = new Map();

    const formatMoney = value => new Intl.NumberFormat('vi-VN', {
        maximumFractionDigits: 0
    }).format(value) + ' ' + currency;

    const setMainImage = url => {
        if (!url || !mainImage || mainImage.tagName !== 'IMG') return;
        mainImage.src = url;
    };

    const updateQuantityBounds = availableStock => {
        if (!quantityInput) return;

        const configuredMin = Number(quantityInput.min || 1);
        const configuredMax = Number(quantityInput.max || availableStock || configuredMin);
        const max = Math.max(
            configuredMin,
            Math.min(configuredMax, Math.max(availableStock, configuredMin)));

        quantityInput.max = String(max);
        quantityInput.value = String(Math.min(
            Math.max(Number(quantityInput.value || configuredMin), configuredMin),
            max));
    };

    const findSelectedVariant = () => {
        if (!hasVariants || selectedValues.size !== optionGroups.length) {
            return null;
        }

        const selectedIds = [...selectedValues.values()]
            .map(Number)
            .sort((a, b) => a - b);

        return variants.find(variant => {
            const ids = [...variant.optionValueIds]
                .map(Number)
                .sort((a, b) => a - b);

            return ids.length === selectedIds.length &&
                ids.every((value, index) => value === selectedIds[index]);
        }) || null;
    };

    const refreshSummary = () => {
        const variant = findSelectedVariant();
        if (!hasVariants) return;

        if (!variant) {
            if (salePrice) salePrice.textContent = 'Chọn đủ phân loại để xem giá';
            if (listPrice) listPrice.hidden = true;
            return;
        }

        if (sku) sku.textContent = variant.sku;
        if (stock) stock.textContent = String(variant.stockQuantity);
        updateQuantityBounds(variant.stockQuantity);
        setMainImage(variant.imageUrl);

        if (salePrice) {
            salePrice.textContent = variant.salePrice == null
                ? 'Liên hệ để nhận giá'
                : formatMoney(variant.salePrice);
        }

        if (listPrice) {
            const showListPrice = variant.listPrice != null &&
                variant.salePrice != null &&
                variant.listPrice > variant.salePrice;
            listPrice.hidden = !showListPrice;
            listPrice.textContent = showListPrice
                ? formatMoney(variant.listPrice)
                : '';
        }
    };

    optionGroups.forEach(group => {
        const optionId = Number(group.dataset.optionId);
        const selectedName = group.querySelector('[data-selected-option-name]');

        group.querySelectorAll('[data-option-value-id]').forEach(button => {
            button.addEventListener('click', () => {
                group.querySelectorAll('[data-option-value-id]')
                    .forEach(item => item.classList.remove('is-selected'));

                button.classList.add('is-selected');
                selectedValues.set(
                    optionId,
                    Number(button.dataset.optionValueId));

                if (selectedName) {
                    selectedName.textContent =
                        button.dataset.optionValueName || button.textContent.trim();
                }

                refreshSummary();
            });
        });
    });

    document.querySelectorAll('[data-gallery-image]').forEach(button => {
        button.addEventListener('click', () => {
            document.querySelectorAll('[data-gallery-image]')
                .forEach(item => item.classList.remove('is-active'));
            button.classList.add('is-active');
            setMainImage(button.dataset.galleryImage);
        });
    });

    minusButton?.addEventListener('click', () => {
        if (!quantityInput) return;
        quantityInput.stepDown();
        quantityInput.dispatchEvent(new Event('change'));
    });

    plusButton?.addEventListener('click', () => {
        if (!quantityInput) return;
        quantityInput.stepUp();
        quantityInput.dispatchEvent(new Event('change'));
    });

    quantityInput?.addEventListener('change', () => {
        const min = Number(quantityInput.min || 1);
        const max = Number(quantityInput.max || min);
        const value = Number(quantityInput.value || min);
        quantityInput.value = String(Math.min(Math.max(value, min), max));
    });

    if (hasVariants) {
        variants
            .find(variant => variant.isDefault)
            ?.optionValueIds
            ?.forEach(valueId => {
                root.querySelector(
                    `[data-option-value-id="${valueId}"]`)?.click();
            });
    }
})();
