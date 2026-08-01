(() => {
    const root = document.querySelector('[data-product-details]');
    if (!root) return;

    const productId = Number(root.dataset.productId || 0);
    const currency = root.dataset.currency || 'VND';
    const hasVariants = root.dataset.hasVariants === 'true';
    const simplePriceAvailable =
        root.dataset.simplePriceAvailable === 'true';
    const simpleStock = Number(root.dataset.simpleStock || 0);
    const minPurchase = Math.max(
        Number(root.dataset.minPurchase || 1),
        1);
    const maxPurchase = root.dataset.maxPurchase
        ? Number(root.dataset.maxPurchase)
        : null;

    const variantScript = document.getElementById('store-variant-data');
    const variants = variantScript
        ? JSON.parse(variantScript.textContent || '[]')
        : [];

    const salePrice = root.querySelector('[data-sale-price]');
    const listPrice = root.querySelector('[data-list-price]');
    const sku = root.querySelector('[data-product-sku]');
    const stock = root.querySelector('[data-product-stock]');
    const mainImage = root.querySelector('[data-main-product-image]');
    const fallbackImageUrl =
        mainImage?.tagName === 'IMG'
            ? mainImage.src
            : '';
    const quantityInput = root.querySelector('[data-quantity-input]');
    const minusButton = root.querySelector('[data-quantity-minus]');
    const plusButton = root.querySelector('[data-quantity-plus]');
    const addButton = root.querySelector('[data-add-to-cart]');
    const cartFeedback = root.querySelector('[data-cart-feedback]');
    const optionGroups = [...root.querySelectorAll('[data-option-id]')];
    const selectedValues = new Map();

    let selectedVariant = null;
    let addingToCart = false;

    const formatMoney = value => new Intl.NumberFormat('vi-VN', {
        maximumFractionDigits: 0
    }).format(value) + ' ' + currency;

    const setMainImage = url => {
        if (!mainImage ||
            mainImage.tagName !== 'IMG') {
            return;
        }

        const targetUrl =
            url || fallbackImageUrl;

        if (!targetUrl) return;

        mainImage.src = targetUrl;

        document
            .querySelectorAll('[data-gallery-image]')
            .forEach(button => {
                button.classList.toggle(
                    'is-active',
                    button.dataset.galleryImage ===
                        targetUrl);
            });
    };

    const setFeedback = (message, state = '') => {
        if (!cartFeedback) return;
        cartFeedback.textContent = message;
        cartFeedback.dataset.state = state;
    };

    const setAddButton = (enabled, text) => {
        if (!addButton) return;
        addButton.disabled = !enabled || addingToCart;
        addButton.textContent = addingToCart
            ? 'Đang thêm...'
            : text;
    };

    const updateQuantityBounds = availableStock => {
        if (!quantityInput) return;

        const policyMax = maxPurchase ?? availableStock;
        const max = Math.min(availableStock, policyMax);

        quantityInput.min = String(minPurchase);
        quantityInput.max = String(Math.max(max, minPurchase));
        quantityInput.value = String(Math.min(
            Math.max(
                Number(quantityInput.value || minPurchase),
                minPurchase),
            Math.max(max, minPurchase)));
    };

    const refreshPurchaseState = () => {
        const purchase = hasVariants
            ? selectedVariant
            : {
                id: null,
                stockQuantity: simpleStock,
                salePrice: simplePriceAvailable ? 1 : null
            };

        if (hasVariants && !purchase) {
            setAddButton(false, 'Chọn phân loại');
            return;
        }

        if (purchase.salePrice == null) {
            setAddButton(false, 'Chưa có giá bán');
            return;
        }

        if (purchase.stockQuantity < minPurchase) {
            setAddButton(false, 'Tạm hết hàng');
            return;
        }

        updateQuantityBounds(purchase.stockQuantity);
        setAddButton(true, 'Thêm vào giỏ hàng');
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
        selectedVariant = findSelectedVariant();

        if (!hasVariants) {
            refreshPurchaseState();
            return;
        }

        if (!selectedVariant) {
            if (salePrice) {
                salePrice.textContent =
                    'Chọn đủ phân loại để xem giá';
            }
            if (listPrice) listPrice.hidden = true;
            refreshPurchaseState();
            return;
        }

        if (sku) sku.textContent = selectedVariant.sku;
        if (stock) {
            stock.textContent = String(
                selectedVariant.stockQuantity);
        }

        updateQuantityBounds(selectedVariant.stockQuantity);
        setMainImage(selectedVariant.imageUrl);

        if (salePrice) {
            salePrice.textContent = selectedVariant.salePrice == null
                ? 'Liên hệ để nhận giá'
                : formatMoney(selectedVariant.salePrice);
        }

        if (listPrice) {
            const showListPrice =
                selectedVariant.listPrice != null &&
                selectedVariant.salePrice != null &&
                selectedVariant.listPrice >
                    selectedVariant.salePrice;

            listPrice.hidden = !showListPrice;
            listPrice.textContent = showListPrice
                ? formatMoney(selectedVariant.listPrice)
                : '';
        }

        setFeedback('');
        refreshPurchaseState();
    };

    optionGroups.forEach(group => {
        const optionId = Number(group.dataset.optionId);
        const selectedName = group.querySelector(
            '[data-selected-option-name]');

        group.querySelectorAll('[data-option-value-id]')
            .forEach(button => {
                button.addEventListener('click', () => {
                    group.querySelectorAll(
                        '[data-option-value-id]')
                        .forEach(item =>
                            item.classList.remove('is-selected'));

                    button.classList.add('is-selected');
                    selectedValues.set(
                        optionId,
                        Number(button.dataset.optionValueId));

                    if (selectedName) {
                        selectedName.textContent =
                            button.dataset.optionValueName ||
                            button.textContent.trim();
                    }

                    refreshSummary();
                });
            });
    });

    document.querySelectorAll('[data-gallery-image]')
        .forEach(button => {
            button.addEventListener('click', () => {
                document.querySelectorAll('[data-gallery-image]')
                    .forEach(item =>
                        item.classList.remove('is-active'));

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
        const min = Number(quantityInput.min || minPurchase);
        const max = Number(quantityInput.max || min);
        const value = Number(quantityInput.value || min);
        quantityInput.value = String(
            Math.min(Math.max(value, min), max));
    });

    addButton?.addEventListener('click', async () => {
        if (addingToCart || addButton.disabled) return;

        const quantity = Number(
            quantityInput?.value || minPurchase);
        const token = document.querySelector(
            '#store-antiforgery input[name="__RequestVerificationToken"]')
            ?.value;

        if (!token) {
            setFeedback(
                'Không tìm thấy mã bảo vệ request. Hãy tải lại trang.',
                'error');
            return;
        }

        const body = new URLSearchParams();
        body.set('ProductId', String(productId));
        body.set('Quantity', String(quantity));
        body.set('__RequestVerificationToken', token);

        if (selectedVariant?.id) {
            body.set(
                'ProductVariantId',
                String(selectedVariant.id));
        }

        addingToCart = true;
        setFeedback('');
        refreshPurchaseState();

        try {
            const response = await fetch('/gio-hang/them', {
                method: 'POST',
                headers: {
                    'Content-Type':
                        'application/x-www-form-urlencoded;charset=UTF-8',
                    Accept: 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body
            });

            const data = await response.json()
                .catch(() => ({}));

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    'Không thể thêm sản phẩm vào giỏ hàng.');
            }

            setFeedback(
                data.message ||
                'Đã thêm sản phẩm vào giỏ hàng.',
                'success');

            await window.mayHomeCart?.refresh?.();
        } catch (error) {
            setFeedback(
                error?.message ||
                'Không thể thêm sản phẩm vào giỏ hàng.',
                'error');
        } finally {
            addingToCart = false;
            refreshPurchaseState();
        }
    });

    if (hasVariants) {
        variants
            .find(variant => variant.isDefault)
            ?.optionValueIds
            ?.forEach(valueId => {
                root.querySelector(
                    `[data-option-value-id="${valueId}"]`)
                    ?.click();
            });
    } else {
        refreshPurchaseState();
    }
})();
