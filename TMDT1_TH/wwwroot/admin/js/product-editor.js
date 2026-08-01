(() => {
    const form = document.querySelector('[data-product-form]');
    if (!form) return;

    const productId = form.dataset.productId || '';
    const hasVariants = document.querySelector('[data-product-has-variants]');
    const variantFields = document.querySelector('[data-product-variant-fields]');
    const simpleFields = document.querySelector('[data-product-simple-fields]');
    const variantBody = document.querySelector('[data-product-variant-body]');
    const variantCount = document.querySelector('[data-product-variant-count]');
    const optionName1 = document.getElementById('OptionName1');
    const optionValues1 = document.getElementById('OptionValues1');
    const optionName2 = document.getElementById('OptionName2');
    const optionValues2 = document.getElementById('OptionValues2');
    const skuInput = document.getElementById('Sku');
    const modelInput = document.getElementById('ModelNumber');
    const nameInput = document.getElementById('Name');
    const categorySelect = document.querySelector('[data-product-category]');
    const brandSelect = document.querySelector('[data-product-brand]');
    const productCodeStatus = document.querySelector('[data-product-code-status]');
    const productWeight = document.getElementById('Weight');
    const lowStockThreshold = document.getElementById('LowStockThreshold');
    const marketSelect = document.querySelector('[data-product-market]');

    const seedElement = document.getElementById('product-variant-seed');
    let seedVariants = [];
    try {
        seedVariants = JSON.parse(seedElement?.textContent || '[]');
    } catch {
        seedVariants = [];
    }

    const escapeHtml = (value) => String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');

    const splitValues = (value) => {
        const result = [];
        const seen = new Set();
        String(value || '').split(',').forEach((item) => {
            const trimmed = item.trim();
            const key = trimmed.toLocaleLowerCase('vi');
            if (trimmed && !seen.has(key)) {
                seen.add(key);
                result.push(trimmed);
            }
        });
        return result;
    };

    const token = (value) => String(value || '')
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

    const productCodeState = {
        timer: null,
        requestId: 0
    };

    const setProductCodeStatus = (message, state = '') => {
        if (!productCodeStatus) return;
        productCodeStatus.textContent = message;
        productCodeStatus.dataset.state = state;
    };

    const loadSystemManagedProductCodes = async () => {
        const name = nameInput?.value.trim() || '';
        const categoryId = categorySelect?.value || '';
        const brandId = brandSelect?.value || '';

        if (!productId && (!name || !categoryId || !brandId)) {
            if (skuInput) skuInput.value = '';
            if (modelInput) modelInput.value = '';
            setProductCodeStatus(
                'Nhập tên sản phẩm, chọn danh mục và thương hiệu để hệ thống tạo mã.',
                '');
            return;
        }

        const requestId = ++productCodeState.requestId;
        const url = new URL('/Admin/Products/GenerateCodes', window.location.origin);

        if (productId) {
            url.searchParams.set('productId', productId);
        } else {
            url.searchParams.set('name', name);
            url.searchParams.set('categoryId', categoryId);
            url.searchParams.set('brandId', brandId);
        }

        setProductCodeStatus(
            'Hệ thống đang tạo và kiểm tra mã trong database...',
            'loading');

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const data = await response.json().catch(() => ({}));

            if (requestId !== productCodeState.requestId) return;
            if (!response.ok) {
                throw new Error(data.message || 'Không thể tạo mã hệ thống.');
            }

            if (skuInput) skuInput.value = data.sku || '';
            if (modelInput) modelInput.value = data.modelNumber || '';

            if (hasVariants?.checked) {
                renderVariantRows();
            }

            setProductCodeStatus(
                productId
                    ? 'SKU và mã model đã được khóa; không thể chỉnh sửa.'
                    : 'Đây là mã xem trước. Server sẽ xác nhận lại mã duy nhất khi lưu.',
                'success');
        } catch (error) {
            if (requestId === productCodeState.requestId) {
                setProductCodeStatus(
                    error?.message || 'Không thể tạo mã hệ thống.',
                    'error');
            }
        }
    };

    const scheduleSystemManagedProductCodes = () => {
        if (productId) return;
        window.clearTimeout(productCodeState.timer);
        productCodeState.timer = window.setTimeout(
            loadSystemManagedProductCodes,
            450);
    };

    nameInput?.addEventListener('input', scheduleSystemManagedProductCodes);
    categorySelect?.addEventListener('change', scheduleSystemManagedProductCodes);
    brandSelect?.addEventListener('change', scheduleSystemManagedProductCodes);

    // Khóa lại ở DOM để các script khác không vô tình mở quyền chỉnh sửa.
    if (skuInput) skuInput.readOnly = true;
    if (modelInput) modelInput.readOnly = true;

    const combinationKey = (value1, value2) => value2
        ? `1=${token(value1)}|2=${token(value2)}`
        : `1=${token(value1)}`;

    const buildCombinations = () => {
        const first = splitValues(optionValues1?.value);
        const second = splitValues(optionValues2?.value);
        if (!first.length) return [];
        if (!second.length) return first.map((value1) => ({ value1, value2: '' }));
        return first.flatMap((value1) => second.map((value2) => ({ value1, value2 })));
    };

    const readVariantRows = () => {
        const map = new Map();
        variantBody?.querySelectorAll('tr[data-variant-row]').forEach((row) => {
            const value = (selector) => row.querySelector(selector)?.value ?? '';
            const checked = (selector) => { const control = row.querySelector(selector); return control?.type === 'hidden' ? control.value === 'true' : (control?.checked ?? false); };
            const key = value('[data-field="combinationKey"]');
            map.set(key, {
                id: value('[data-field="id"]') || null,
                priceScheduleId: value('[data-field="priceScheduleId"]') || null,
                combinationKey: key,
                value1: value('[data-field="value1"]'),
                value2: value('[data-field="value2"]'),
                name: value('[data-field="name"]'),
                sku: value('[data-field="sku"]'),
                barcode: value('[data-field="barcode"]'),
                costPrice: value('[data-field="costPrice"]'),
                listPrice: value('[data-field="listPrice"]'),
                salePrice: value('[data-field="salePrice"]'),
                stockQuantity: value('[data-field="stockQuantity"]'),
                lowStockThreshold: value('[data-field="lowStockThreshold"]'),
                weight: value('[data-field="weight"]'),
                isDefault: checked('[data-field="isDefault"]'),
                isActive: checked('[data-field="isActive"]')
            });
        });
        return map;
    };

    const normalizeSeed = (item) => {
        const value1 = item.value1 || '';
        const value2 = item.value2 || '';

        // Không tin CombinationKey cũ vì dữ liệu seed và editor từng
        // sử dụng hai cách chuẩn hóa khác nhau. Khóa mới được tạo từ
        // chính giá trị phân loại để dòng cũ khớp đúng khi render lại.
        const canonicalKey = value1
            ? combinationKey(value1, value2)
            : (item.combinationKey || '');

        return {
            id: item.id ?? null,
            priceScheduleId: item.priceScheduleId ?? null,
            combinationKey: canonicalKey,
            value1,
            value2,
            name: item.name || [value1, value2].filter(Boolean).join(' / '),
            sku: item.sku || '',
            barcode: item.barcode || '',
            costPrice: item.costPrice ?? 0,
            listPrice: item.listPrice ?? 0,
            salePrice: item.salePrice ?? 0,
            stockQuantity: item.stockQuantity ?? 0,
            lowStockThreshold: item.lowStockThreshold ?? Number(lowStockThreshold?.value || 5),
            weight: item.weight ?? Number(productWeight?.value || 0),
            isDefault: Boolean(item.isDefault),
            isActive: item.isActive !== false
        };
    };

    let variantState = new Map(seedVariants.map((item) => {
        const normalized = normalizeSeed(item);
        return [normalized.combinationKey, normalized];
    }));

    const renderVariantRows = (preserveTable = true) => {
        if (!variantBody || !variantCount) return;
        if (preserveTable) {
            const current = readVariantRows();
            current.forEach((value, key) => variantState.set(key, value));
        }

        const combinations = buildCombinations();
        const defaultLowStock = Number(lowStockThreshold?.value || 5);
        const defaultWeight = Number(productWeight?.value || 0);
        let hasDefault = false;

        const rows = combinations.map((combination, index) => {
            const key = combinationKey(combination.value1, combination.value2);
            const previous = variantState.get(key) || {};
            const item = {
                id: previous.id || '',
                priceScheduleId: previous.priceScheduleId || '',
                combinationKey: key,
                value1: combination.value1,
                value2: combination.value2,
                name: [combination.value1, combination.value2].filter(Boolean).join(' / '),
                sku: previous.sku || `${baseSku()}-${token(combination.value1)}${combination.value2 ? `-${token(combination.value2)}` : ''}-${String(index + 1).padStart(2, '0')}`,
                barcode: previous.barcode || '',
                costPrice: previous.costPrice ?? 0,
                listPrice: previous.listPrice ?? 0,
                salePrice: previous.salePrice ?? 0,
                stockQuantity: previous.stockQuantity ?? 0,
                lowStockThreshold: previous.lowStockThreshold ?? defaultLowStock,
                weight: previous.weight ?? defaultWeight,
                isDefault: Boolean(previous.isDefault),
                isActive: previous.isActive !== false
            };
            if (item.isDefault && item.isActive && !hasDefault) hasDefault = true;
            else if (item.isDefault) item.isDefault = false;
            return item;
        });

        if (!hasDefault) {
            const firstActive = rows.find((item) => item.isActive);
            if (firstActive) firstActive.isDefault = true;
        }

        variantState = new Map(rows.map((item) => [item.combinationKey, item]));
        variantCount.textContent = `${rows.length} biến thể`;
        variantBody.innerHTML = rows.map((item, index) => variantRowHtml(item, index)).join('');
        bindVariantRowEvents();
        updateReadiness();
    };

    const hiddenInput = (name, value, field) => `<input type="hidden" name="${name}" value="${escapeHtml(value)}" data-field="${field}" />`;

    const variantRowHtml = (item, index) => `
        <tr data-variant-row class="variant-card-row">
            <td colspan="11">
                <article class="variant-card ${item.isActive ? '' : 'is-inactive'}">
                    ${hiddenInput(`Variants[${index}].Id`, item.id || '', 'id')}
                    ${hiddenInput(`Variants[${index}].PriceScheduleId`, item.priceScheduleId || '', 'priceScheduleId')}
                    ${hiddenInput(`Variants[${index}].CombinationKey`, item.combinationKey, 'combinationKey')}
                    ${hiddenInput(`Variants[${index}].Value1`, item.value1, 'value1')}
                    ${hiddenInput(`Variants[${index}].Value2`, item.value2 || '', 'value2')}
                    ${hiddenInput(`Variants[${index}].Name`, item.name, 'name')}

                    <header class="variant-card__header">
                        <div class="variant-card__identity">
                            <span class="variant-card__number">${index + 1}</span>
                            <div>
                                <strong>${escapeHtml(item.name)}</strong>
                                <small data-variant-summary-sku>
                                    ${escapeHtml(item.sku || 'Chưa có SKU')}
                                </small>
                            </div>
                        </div>

                        <div class="variant-card__controls">
                            <label class="variant-default-choice">
                                <input type="radio"
                                       name="variant-default-ui"
                                       ${item.isDefault ? 'checked' : ''}
                                       data-variant-default-radio />
                                <input type="hidden"
                                       name="Variants[${index}].IsDefault"
                                       value="${item.isDefault ? 'true' : 'false'}"
                                       data-field="isDefault" />
                                Mặc định
                            </label>

                            <label class="variant-active-choice">
                                <span>Bán</span>
                                <span class="mini-switch">
                                    <input type="checkbox"
                                           name="Variants[${index}].IsActive"
                                           value="true"
                                           ${item.isActive ? 'checked' : ''}
                                           data-field="isActive" />
                                    <i></i>
                                </span>
                                <input type="hidden"
                                       name="Variants[${index}].IsActive"
                                       value="false" />
                            </label>

                            <button class="btn btn-light btn-sm"
                                    type="button"
                                    data-toggle-variant-details
                                    aria-expanded="false">
                                <i class="bi bi-sliders"></i>
                                Chi tiết
                            </button>
                        </div>
                    </header>

                    <div class="variant-card__quick-fields">
                        <label class="form-field">
                            <span>Giá bán</span>
                            <div class="input-suffix">
                                <input class="table-input"
                                       type="number"
                                       min="0"
                                       step="1"
                                       name="Variants[${index}].SalePrice"
                                       value="${escapeHtml(item.salePrice)}"
                                       data-field="salePrice" />
                                <i>₫</i>
                            </div>
                        </label>

                        <label class="form-field">
                            <span>Giá niêm yết</span>
                            <div class="input-suffix">
                                <input class="table-input"
                                       type="number"
                                       min="0"
                                       step="1"
                                       name="Variants[${index}].ListPrice"
                                       value="${escapeHtml(item.listPrice)}"
                                       data-field="listPrice" />
                                <i>₫</i>
                            </div>
                        </label>

                        <label class="form-field">
                            <span>Tồn kho</span>
                            <input class="table-input"
                                   type="number"
                                   min="0"
                                   step="1"
                                   name="Variants[${index}].StockQuantity"
                                   value="${escapeHtml(item.stockQuantity)}"
                                   data-field="stockQuantity" />
                        </label>
                    </div>

                    <div class="variant-card__details"
                         data-variant-details
                         hidden>
                        <label class="form-field">
                            <span>SKU biến thể</span>
                            <input class="table-input variant-sku-input"
                                   name="Variants[${index}].Sku"
                                   value="${escapeHtml(item.sku)}"
                                   maxlength="100"
                                   required
                                   data-field="sku" />
                        </label>

                        <label class="form-field">
                            <span>Barcode</span>
                            <input class="table-input"
                                   name="Variants[${index}].Barcode"
                                   value="${escapeHtml(item.barcode)}"
                                   maxlength="100"
                                   data-field="barcode" />
                        </label>

                        <label class="form-field">
                            <span>Giá vốn</span>
                            <div class="input-suffix">
                                <input class="table-input"
                                       type="number"
                                       min="0"
                                       step="1"
                                       name="Variants[${index}].CostPrice"
                                       value="${escapeHtml(item.costPrice)}"
                                       data-field="costPrice" />
                                <i>₫</i>
                            </div>
                        </label>

                        <label class="form-field">
                            <span>Ngưỡng cảnh báo tồn</span>
                            <input class="table-input"
                                   type="number"
                                   min="0"
                                   step="1"
                                   name="Variants[${index}].LowStockThreshold"
                                   value="${escapeHtml(item.lowStockThreshold)}"
                                   data-field="lowStockThreshold" />
                        </label>

                        <label class="form-field">
                            <span>Khối lượng</span>
                            <div class="input-suffix">
                                <input class="table-input"
                                       type="number"
                                       min="0"
                                       step="0.001"
                                       name="Variants[${index}].Weight"
                                       value="${escapeHtml(item.weight)}"
                                       data-field="weight" />
                                <i>kg</i>
                            </div>
                        </label>
                    </div>
                </article>
            </td>
        </tr>`;

    const bindVariantRowEvents = () => {
        variantBody?.querySelectorAll('[data-variant-default-radio]').forEach((radio) => {
            radio.addEventListener('change', () => {
                variantBody
                    .querySelectorAll('[data-field="isDefault"]')
                    .forEach((hidden) => {
                        hidden.value = 'false';
                    });

                const row = radio.closest('[data-variant-row]');
                const hiddenDefault =
                    row?.querySelector('[data-field="isDefault"]');

                if (hiddenDefault) hiddenDefault.value = 'true';
            });
        });

        variantBody
            ?.querySelectorAll('[data-toggle-variant-details]')
            .forEach((button) => {
                button.addEventListener('click', () => {
                    const row =
                        button.closest('[data-variant-row]');

                    const details =
                        row?.querySelector('[data-variant-details]');

                    if (!details) return;

                    const expanded = details.hidden;
                    details.hidden = !expanded;
                    button.setAttribute(
                        'aria-expanded',
                        String(expanded));

                    button.innerHTML = expanded
                        ? '<i class="bi bi-chevron-up"></i> Thu gọn'
                        : '<i class="bi bi-sliders"></i> Chi tiết';
                });
            });

        variantBody?.querySelectorAll('input').forEach((input) => {
            const updateRow = () => {
                const row =
                    input.closest('[data-variant-row]');

                validateVariantRow(row);

                if (input.matches('[data-field="sku"]')) {
                    const summary =
                        row?.querySelector(
                            '[data-variant-summary-sku]');

                    if (summary) {
                        summary.textContent =
                            input.value.trim() ||
                            'Chưa có SKU';
                    }
                }

                if (input.matches('[data-field="isActive"]')) {
                    row?.querySelector('.variant-card')
                        ?.classList.toggle(
                            'is-inactive',
                            !input.checked);
                }

                updateReadiness();
            };

            input.addEventListener('input', updateRow);
            input.addEventListener('change', updateRow);
        });

        variantBody
            ?.querySelectorAll('[data-variant-row]')
            .forEach(validateVariantRow);
    };    const validateVariantRow = (row) => {
        if (!row) return true;
        const active = row.querySelector('[data-field="isActive"]')?.checked ?? false;
        const list = row.querySelector('[data-field="listPrice"]');
        const sale = row.querySelector('[data-field="salePrice"]');
        const weight = row.querySelector('[data-field="weight"]');
        const sku = row.querySelector('[data-field="sku"]');
        const invalidPrice = active && Number(sale?.value || 0) > Number(list?.value || 0);
        sale?.setCustomValidity(invalidPrice ? 'Giá bán không được lớn hơn giá niêm yết.' : '');
        sku?.setCustomValidity(active && !sku.value.trim() ? 'Biến thể hoạt động cần SKU.' : '');
        weight?.setCustomValidity(active && isPublishing() && Number(weight.value || 0) <= 0 ? 'Biến thể đang bán cần khối lượng lớn hơn 0.' : '');
        return !invalidPrice;
    };

    const refreshVariantMode = () => {
        const enabled = hasVariants?.checked ?? false;
        if (variantFields) variantFields.hidden = !enabled;
        if (simpleFields) simpleFields.hidden = enabled;
        if (enabled) renderVariantRows();
        updateReadiness();
    };

    hasVariants?.addEventListener('change', refreshVariantMode);
    document.querySelector('[data-generate-product-variants]')?.addEventListener('click', () => renderVariantRows());
    [optionValues1, optionValues2, optionName1, optionName2, skuInput, nameInput]
        .filter(Boolean)
        .forEach((input) => input.addEventListener('input', () => {
            if (hasVariants?.checked) renderVariantRows();
            updateReadiness();
        }));

    document.querySelector('[data-fill-variant-prices]')?.addEventListener('click', () => {
        const rows = [...(variantBody?.querySelectorAll('[data-variant-row]') || [])];
        if (!rows.length) return;
        const first = rows[0];
        let cost = first.querySelector('[data-field="costPrice"]')?.value || '';
        let list = first.querySelector('[data-field="listPrice"]')?.value || '';
        let sale = first.querySelector('[data-field="salePrice"]')?.value || '';
        if (!cost && !list && !sale) {
            cost = window.prompt('Giá vốn áp dụng cho tất cả SKU:', '0') ?? '';
            list = window.prompt('Giá niêm yết áp dụng cho tất cả SKU:', '0') ?? '';
            sale = window.prompt('Giá bán áp dụng cho tất cả SKU:', '0') ?? '';
        }
        rows.forEach((row) => {
            row.querySelector('[data-field="costPrice"]').value = cost;
            row.querySelector('[data-field="listPrice"]').value = list;
            row.querySelector('[data-field="salePrice"]').value = sale;
            validateVariantRow(row);
        });
        updateReadiness();
    });

    const simpleListPrice = document.querySelector('[data-simple-list-price]');
    const simpleSalePrice = document.querySelector('[data-simple-sale-price]');
    const validateSimplePrice = () => {
        if (!simpleListPrice || !simpleSalePrice) return;
        const invalid = Number(simpleSalePrice.value || 0) > Number(simpleListPrice.value || 0);
        simpleSalePrice.setCustomValidity(invalid ? 'Giá bán không được lớn hơn giá niêm yết.' : '');
    };
    simpleListPrice?.addEventListener('input', validateSimplePrice);
    simpleSalePrice?.addEventListener('input', validateSimplePrice);

    marketSelect?.addEventListener('change', async () => {
        if (!productId || !marketSelect.value) return;
        try {
            const response = await fetch(`/Admin/Products/PricesForMarket?productId=${encodeURIComponent(productId)}&marketId=${encodeURIComponent(marketSelect.value)}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!response.ok) return;
            const data = await response.json();
            const setValue = (id, value) => {
                const input = document.getElementById(id);
                if (input) input.value = value ?? '';
            };
            setValue('ProductPriceScheduleId', data.product.priceScheduleId);
            setValue('CostPrice', data.product.costPrice);
            setValue('ListPrice', data.product.listPrice);
            setValue('SalePrice', data.product.salePrice);
            setValue('ValidFrom', data.product.validFrom);
            setValue('ValidTo', data.product.validTo);
            setValue('PriceNote', data.product.note);

            const priceByVariantId = new Map((data.variants || []).map((item) => [String(item.variantId), item]));
            variantBody?.querySelectorAll('[data-variant-row]').forEach((row) => {
                const id = row.querySelector('[data-field="id"]')?.value;
                const item = priceByVariantId.get(String(id));
                if (!item) return;
                row.querySelector('[data-field="priceScheduleId"]').value = item.priceScheduleId || '';
                row.querySelector('[data-field="costPrice"]').value = item.costPrice ?? 0;
                row.querySelector('[data-field="listPrice"]').value = item.listPrice ?? 0;
                row.querySelector('[data-field="salePrice"]').value = item.salePrice ?? 0;
            });
            updateReadiness();
        } catch {
            // Giữ nguyên dữ liệu đang nhập nếu tải giá theo thị trường thất bại.
        }
    });

    const imageInput = document.querySelector('[data-product-images]');
    const imagePreview = document.querySelector('[data-new-image-preview]');
    const validateImages = async () => {
        if (!imageInput || !imagePreview) return;
        imagePreview.innerHTML = '';
        imageInput.setCustomValidity('');
        const files = [...(imageInput.files || [])];
        const remainingExisting = [...document.querySelectorAll('[data-existing-image]')]
            .filter((card) => !card.querySelector('[name="RemoveImageIds"]')?.checked).length;
        if (files.length + remainingExisting > 9) {
            imageInput.setCustomValidity('Mỗi sản phẩm được tải tối đa 9 ảnh.');
        }

        for (const file of files) {
            const card = document.createElement('article');
            card.className = 'new-image-card';
            const img = document.createElement('img');
            const meta = document.createElement('small');
            const url = URL.createObjectURL(file);
            img.src = url;
            card.append(img, meta);
            imagePreview.appendChild(card);
            await new Promise((resolve) => {
                img.onload = () => {
                    meta.textContent = `${file.name} · ${img.naturalWidth} × ${img.naturalHeight}px`;
                    if (img.naturalWidth < 600 || img.naturalHeight < 600) {
                        card.classList.add('has-warning');
                        imageInput.setCustomValidity('Ảnh sản phẩm cần có kích thước tối thiểu 600 × 600 px.');
                    }
                    URL.revokeObjectURL(url);
                    resolve();
                };
                img.onerror = () => {
                    imageInput.setCustomValidity('Có ảnh không thể đọc được.');
                    URL.revokeObjectURL(url);
                    resolve();
                };
            });
        }
        updateReadiness();
    };
    imageInput?.addEventListener('change', validateImages);
    document.querySelectorAll('[name="RemoveImageIds"]').forEach((input) => input.addEventListener('change', () => {
        input.closest('[data-existing-image]')?.classList.toggle('is-removed', input.checked);
        validateImages();
    }));

    const specificationList = document.querySelector('[data-specification-list]');
    const reindexSpecifications = () => {
        specificationList?.querySelectorAll('[data-specification-row]').forEach((row, index) => {
            row.querySelectorAll('input').forEach((input) => {
                const suffix = input.name.includes('.Name') ? 'Name' : input.name.includes('.Value') ? 'Value' : 'Id';
                input.name = `Specifications[${index}].${suffix}`;
                input.id = `Specifications_${index}__${suffix}`;
            });
        });
    };
    document.querySelector('[data-add-specification]')?.addEventListener('click', () => {
        if (!specificationList) return;
        const index = specificationList.querySelectorAll('[data-specification-row]').length;
        const row = document.createElement('div');
        row.className = 'specification-row';
        row.dataset.specificationRow = '';
        row.innerHTML = `
            <input type="hidden" name="Specifications[${index}].Id" />
            <label class="form-field"><span>Tên thông số</span><input name="Specifications[${index}].Name" maxlength="150" placeholder="Công suất" /></label>
            <label class="form-field"><span>Giá trị</span><input name="Specifications[${index}].Value" maxlength="1000" placeholder="1.700 W" /></label>
            <button class="icon-btn" type="button" data-remove-specification title="Xóa thông số"><i class="bi bi-trash"></i></button>`;
        specificationList.appendChild(row);
    });
    specificationList?.addEventListener('click', (event) => {
        const button = event.target.closest('[data-remove-specification]');
        if (!button) return;
        button.closest('[data-specification-row]')?.remove();
        reindexSpecifications();
        updateReadiness();
    });

    const titleInput = document.querySelector('[data-title-input]');
    const titleCount = document.querySelector('[data-title-count]');
    const descriptionInput = document.querySelector('[data-description-input]');
    const descriptionCount = document.querySelector('[data-description-count]');
    const statusSelect = document.getElementById('Status');
    const isPublishing = () => statusSelect?.value !== '1';

    const updateCounters = () => {
        if (titleCount) titleCount.textContent = String(titleInput?.value.length || 0);
        if (descriptionCount) descriptionCount.textContent = String(descriptionInput?.value.trim().length || 0);
    };
    titleInput?.addEventListener('input', () => { updateCounters(); updateReadiness(); });
    descriptionInput?.addEventListener('input', () => { updateCounters(); updateReadiness(); });
    statusSelect?.addEventListener('change', () => {
        variantBody?.querySelectorAll('[data-variant-row]').forEach(validateVariantRow);
        updateReadiness();
    });

    const readinessScore = document.querySelector('[data-readiness-score]');
    const readinessChecks = document.querySelector('[data-readiness-checks]');
    const inputValue = (id) => document.getElementById(id)?.value.trim() || '';
    const updateReadiness = () => {
        if (!readinessScore || !readinessChecks) return;
        const existingImages = [...document.querySelectorAll('[data-existing-image]')]
            .filter((card) => !card.querySelector('[name="RemoveImageIds"]')?.checked).length;
        const newImages = imageInput?.files?.length || 0;
        const rows = [...(variantBody?.querySelectorAll('[data-variant-row]') || [])];
        const activeRows = rows.filter((row) => row.querySelector('[data-field="isActive"]')?.checked);
        const variantPricesComplete = !hasVariants?.checked || (activeRows.length > 0 && activeRows.every((row) => {
            const list = Number(row.querySelector('[data-field="listPrice"]')?.value || 0);
            const sale = Number(row.querySelector('[data-field="salePrice"]')?.value || 0);
            return list > 0 && sale > 0 && sale <= list;
        }));
        const simplePriceComplete = hasVariants?.checked || (Number(simpleListPrice?.value || 0) > 0 && Number(simpleSalePrice?.value || 0) > 0 && Number(simpleSalePrice?.value || 0) <= Number(simpleListPrice?.value || 0));
        const checks = [
            ['Tên sản phẩm từ 10 ký tự', (titleInput?.value.trim().length || 0) >= 10],
            ['Mô tả chi tiết từ 110 ký tự', (descriptionInput?.value.trim().length || 0) >= 110],
            ['Có ít nhất một hình ảnh', existingImages + newImages >= 1],
            ['Đã chọn danh mục và thương hiệu', Boolean(inputValue('CategoryId') && inputValue('BrandId'))],
            ['Có xuất xứ và nhà sản xuất', Boolean(inputValue('CountryOfOrigin') && inputValue('ManufacturerName') && inputValue('ManufacturerAddress'))],
            ['Có khối lượng và kích thước kiện hàng', Number(inputValue('Weight')) > 0 && Number(inputValue('PackageLengthCm')) > 0 && Number(inputValue('PackageWidthCm')) > 0 && Number(inputValue('PackageHeightCm')) > 0],
            ['SKU và biến thể hợp lệ', !hasVariants?.checked || activeRows.length > 0],
            ['Giá bán hợp lệ', variantPricesComplete && simplePriceComplete],
            ['Đã chọn thị trường', Boolean(marketSelect?.value)],
            ['Có thông số kỹ thuật', Boolean(specificationList?.querySelector('input[name$=".Name"]')?.value.trim())]
        ];
        const passed = checks.filter((item) => item[1]).length;
        readinessScore.textContent = String(passed * 10);
        readinessChecks.innerHTML = checks.map(([label, ok]) => `<span class="${ok ? 'is-ok' : ''}"><i class="bi ${ok ? 'bi-check-circle-fill' : 'bi-circle'}"></i>${escapeHtml(label)}</span>`).join('');
    };

    const pageSaveButtons = [...document.querySelectorAll('[data-product-save-button]')];
    const realSubmitButtons = {
        draft: document.querySelector('[data-product-real-submit="draft"]'),
        save: document.querySelector('[data-product-real-submit="save"]')
    };

    const focusFirstInvalid = () => {
        const invalid = form.querySelector(':invalid');
        if (!invalid) return;
        invalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        window.setTimeout(() => invalid.focus({ preventScroll: true }), 250);
    };

    const requestProductSubmit = (mode) => {
        if (hasVariants?.checked) renderVariantRows();
        validateSimplePrice();
        variantBody?.querySelectorAll('[data-variant-row]').forEach(validateVariantRow);
        if (!form.checkValidity()) {
            form.reportValidity();
            focusFirstInvalid();
            return;
        }
        const submitter = realSubmitButtons[mode] || realSubmitButtons.save;
        if (typeof form.requestSubmit === 'function' && submitter) form.requestSubmit(submitter);
        else form.submit();
    };

    pageSaveButtons.forEach((button) => button.addEventListener('click', () => requestProductSubmit(button.dataset.productSaveButton || 'save')));
    form.addEventListener('submit', (event) => {
        if (hasVariants?.checked) renderVariantRows();
        validateSimplePrice();
        variantBody?.querySelectorAll('[data-variant-row]').forEach(validateVariantRow);
        if (!form.checkValidity()) {
            event.preventDefault();
            form.reportValidity();
            focusFirstInvalid();
            return;
        }
        pageSaveButtons.forEach((button) => {
            button.disabled = true;
            button.classList.add('is-loading');
        });
    });

    updateCounters();
    refreshVariantMode();
    validateSimplePrice();
    updateReadiness();

    loadSystemManagedProductCodes();
})();
