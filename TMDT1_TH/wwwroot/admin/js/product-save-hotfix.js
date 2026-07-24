(() => {
    const form =
        document.querySelector('[data-product-form]');

    if (!form) return;

    const hasVariants =
        document.querySelector(
            '[data-product-has-variants]');

    const variantFields =
        document.querySelector(
            '[data-product-variant-fields]');

    const simpleFields =
        document.querySelector(
            '[data-product-simple-fields]');

    const imageInput =
        document.querySelector(
            '[data-product-images]');

    const saveButtons = [
        ...document.querySelectorAll(
            '[data-product-save-button]')
    ];

    const clearImageRecommendationError = () => {
        if (!imageInput) return;

        const message =
            imageInput.validationMessage || '';

        // Bản cũ biến khuyến nghị 600 x 600 thành lỗi bắt buộc.
        // Chỉ xóa đúng lỗi kích thước này; vẫn giữ lỗi định dạng,
        // dung lượng và số lượng ảnh.
        if (/600\s*[×xX]\s*600/i.test(message) ||
            /kích thước tối thiểu\s*600/i.test(message)) {
            imageInput.setCustomValidity('');
        }
    };

    const synchronizeModeControls = () => {
        const variantsEnabled =
            hasVariants?.checked === true;

        if (variantFields) {
            variantFields.hidden =
                !variantsEnabled;

            variantFields
                .querySelectorAll(
                    'input, select, textarea')
                .forEach(control => {
                    control.disabled =
                        !variantsEnabled;
                });
        }

        if (simpleFields) {
            simpleFields.hidden =
                variantsEnabled;

            simpleFields
                .querySelectorAll(
                    'input, select, textarea')
                .forEach(control => {
                    control.disabled =
                        variantsEnabled;

                    if (variantsEnabled &&
                        typeof control.setCustomValidity ===
                        'function') {
                        control.setCustomValidity('');
                    }
                });
        }

        clearImageRecommendationError();
    };

    const enableSaveButtons = () => {
        saveButtons.forEach(button => {
            button.disabled = false;
            button.removeAttribute('disabled');
        });

        form
            .querySelectorAll(
                '.product-form-actions button[type="submit"]')
            .forEach(button => {
                button.disabled = false;
                button.removeAttribute('disabled');
            });
    };

    // Chạy sau script chính để sửa trạng thái ban đầu.
    const initialize = () => {
        enableSaveButtons();
        synchronizeModeControls();
    };

    initialize();

    window.setTimeout(
        initialize,
        0);

    window.setTimeout(
        initialize,
        250);

    hasVariants?.addEventListener(
        'change',
        () => {
            // Chạy sau listener của product-editor.js để trạng thái
            // cuối cùng luôn đúng.
            window.setTimeout(
                synchronizeModeControls,
                0);
        });

    imageInput?.addEventListener(
        'change',
        () => {
            // Script chính đọc kích thước ảnh bất đồng bộ.
            // Kiểm tra lại sau khi preview hoàn thành.
            window.setTimeout(
                clearImageRecommendationError,
                100);

            window.setTimeout(
                clearImageRecommendationError,
                500);

            window.setTimeout(
                clearImageRecommendationError,
                1200);
        });

    // Capture chạy trước listener click của product-editor.js.
    document.addEventListener(
        'click',
        event => {
            const button =
                event.target.closest(
                    '[data-product-save-button], ' +
                    '[data-product-form] button[type="submit"]');

            if (!button) return;

            enableSaveButtons();
            synchronizeModeControls();
        },
        true);

    // Phòng trường hợp submit bằng Enter.
    form.addEventListener(
        'keydown',
        event => {
            if (event.key !== 'Enter') return;

            enableSaveButtons();
            synchronizeModeControls();
        },
        true);

    form.addEventListener(
        'submit',
        () => {
            synchronizeModeControls();
        },
        true);
})();
