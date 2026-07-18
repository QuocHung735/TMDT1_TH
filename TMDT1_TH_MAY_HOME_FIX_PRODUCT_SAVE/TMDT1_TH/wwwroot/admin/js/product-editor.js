(() => {
    const hasVariants = document.querySelector('[data-product-has-variants]');
    const variantFields = document.querySelector('[data-product-variant-fields]');
    const simpleStock = document.querySelector('[data-product-simple-stock]');
    const variantBody = document.querySelector('[data-product-variant-body]');
    const variantCount = document.querySelector('[data-product-variant-count]');

    const optionValues1 = document.getElementById('OptionValues1');
    const optionValues2 = document.getElementById('OptionValues2');
    const skuInput = document.getElementById('Sku');
    const nameInput = document.getElementById('Name');
    const variantStock = document.getElementById('VariantStockQuantity');

    const splitValues = (value) => [...new Set((value || '')
        .split(',')
        .map(item => item.trim())
        .filter(Boolean))];

    const token = (value) => value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/gi, 'd')
        .replace(/[^a-zA-Z0-9]/g, '')
        .toUpperCase()
        .slice(0, 12) || 'VAR';

    const baseSku = () => {
        const entered = skuInput?.value.trim();
        if (entered) return entered.toUpperCase();
        return `HOME-${token(nameInput?.value || 'SANPHAM')}`;
    };

    const buildCombinations = () => {
        const first = splitValues(optionValues1?.value);
        const second = splitValues(optionValues2?.value);
        if (!first.length) return [];
        if (!second.length) return first.map(value => [value]);
        return first.flatMap(value1 => second.map(value2 => [value1, value2]));
    };

    const renderVariants = () => {
        if (!variantBody || !variantCount) return;
        const combinations = buildCombinations();
        const stock = Math.max(0, Number(variantStock?.value || 0));

        variantCount.textContent = `${combinations.length} biến thể`;
        variantBody.innerHTML = combinations.map((values, index) => {
            const suffix = values.map(token).join('-');
            return `<tr>
                <td><strong>${escapeHtml(values.join(' / '))}</strong></td>
                <td><code>${escapeHtml(`${baseSku()}-${suffix}-${String(index + 1).padStart(2, '0')}`)}</code></td>
                <td>${stock}</td>
            </tr>`;
        }).join('');
    };

    const refreshVariantMode = () => {
        const enabled = hasVariants?.checked ?? false;
        if (variantFields) variantFields.style.display = enabled ? '' : 'none';
        if (simpleStock) simpleStock.style.display = enabled ? 'none' : '';
        if (enabled) renderVariants();
    };

    const escapeHtml = (value) => value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');

    hasVariants?.addEventListener('change', refreshVariantMode);
    [optionValues1, optionValues2, skuInput, nameInput, variantStock]
        .filter(Boolean)
        .forEach(input => input.addEventListener('input', renderVariants));

    refreshVariantMode();

    const imageInput = document.querySelector('[data-product-image-input]');
    const imagePreview = document.querySelector('[data-product-image-preview]');
    const imageEmpty = document.querySelector('[data-product-image-empty]');

    imageInput?.addEventListener('change', () => {
        const file = imageInput.files?.[0];
        if (!file || !imagePreview) return;

        const objectUrl = URL.createObjectURL(file);
        imagePreview.src = objectUrl;
        imagePreview.style.display = '';
        if (imageEmpty) imageEmpty.style.display = 'none';
        imagePreview.onload = () => URL.revokeObjectURL(objectUrl);
    });

    const listPrice = document.querySelector('[data-product-list-price]');
    const salePrice = document.querySelector('[data-product-sale-price]');
    const validatePrice = () => {
        if (!listPrice || !salePrice) return;
        const invalid = Number(salePrice.value || 0) > Number(listPrice.value || 0);
        salePrice.setCustomValidity(invalid ? 'Giá bán không được lớn hơn giá niêm yết.' : '');
    };

    listPrice?.addEventListener('input', validatePrice);
    salePrice?.addEventListener('input', validatePrice);
    validatePrice();

    const productForm = document.querySelector('[data-product-form]');
    const pageSaveButtons = [...document.querySelectorAll('[data-product-save-button]')];
    const realSubmitButtons = {
        draft: document.querySelector('[data-product-real-submit="draft"]'),
        save: document.querySelector('[data-product-real-submit="save"]')
    };

    const firstInvalidControl = () => productForm?.querySelector(':invalid');

    const focusFirstInvalid = () => {
        const invalid = firstInvalidControl();
        if (!invalid) return;
        invalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        window.setTimeout(() => invalid.focus({ preventScroll: true }), 250);
    };

    const requestProductSubmit = (mode) => {
        if (!productForm) return;
        validatePrice();

        if (!productForm.checkValidity()) {
            productForm.reportValidity();
            focusFirstInvalid();
            return;
        }

        const submitter = realSubmitButtons[mode] || realSubmitButtons.save;
        if (typeof productForm.requestSubmit === 'function' && submitter) {
            productForm.requestSubmit(submitter);
            return;
        }

        if (mode === 'draft') {
            const hiddenMode = document.createElement('input');
            hiddenMode.type = 'hidden';
            hiddenMode.name = 'mode';
            hiddenMode.value = 'draft';
            productForm.appendChild(hiddenMode);
        }
        productForm.submit();
    };

    pageSaveButtons.forEach(button => {
        button.addEventListener('click', () => {
            requestProductSubmit(button.dataset.productSaveButton || 'save');
        });
    });

    productForm?.addEventListener('submit', (event) => {
        validatePrice();
        if (!productForm.checkValidity()) {
            event.preventDefault();
            productForm.reportValidity();
            focusFirstInvalid();
            return;
        }

        pageSaveButtons.forEach(button => {
            button.disabled = true;
            button.classList.add('is-loading');
        });
    });

})();
