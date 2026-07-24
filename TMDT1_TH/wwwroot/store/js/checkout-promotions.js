(() => {
    const form =
        document.querySelector(
            "[data-shipping-checkout]");

    if (!form) return;

    const codeInput =
        form.querySelector(
            "[data-checkout-promotion-code]");

    const applyButton =
        form.querySelector(
            "[data-apply-promotion]");

    const message =
        form.querySelector(
            "[data-promotion-message]");

    const discountOutput =
        document.querySelector(
            "[data-checkout-discount]");

    const totalOutput =
        document.querySelector(
            "[data-checkout-total]");

    if (!codeInput ||
        !applyButton ||
        !discountOutput ||
        !totalOutput) {
        return;
    }

    const endpoint =
        form.dataset.promotionUrl;

    const subtotal =
        Number(form.dataset.subtotal || 0);

    const currency =
        form.dataset.currency || "VND";

    const token =
        form.querySelector(
            'input[name="__RequestVerificationToken"]')
            ?.value || "";

    let currentDiscount =
        Number(
            discountOutput.dataset.discount || 0);

    const formatMoney = value =>
        new Intl.NumberFormat("vi-VN")
            .format(Math.max(0, value));

    const shippingFee = () => {
        const selected =
            form.querySelector(
                "[data-shipping-radio]:checked");

        return Number(
            selected?.dataset.shippingFee || 0);
    };

    const updateTotals = () => {
        discountOutput.textContent =
            currentDiscount > 0
                ? `- ${formatMoney(
                    currentDiscount)} ${currency}`
                : `0 ${currency}`;

        totalOutput.textContent =
            `${formatMoney(
                subtotal +
                shippingFee() -
                currentDiscount)} ${currency}`;
    };

    const setMessage = (
        text,
        success) => {
        if (!message) return;

        message.textContent = text || "";

        message.classList.toggle(
            "is-success",
            success === true);

        message.classList.toggle(
            "is-error",
            success === false);
    };

    const appendCartLines = body => {
        document
            .querySelectorAll(
                "[data-promotion-product-id]")
            .forEach(item => {
                body.append(
                    "productIds",
                    item.dataset
                        .promotionProductId);

                body.append(
                    "lineTotals",
                    item.dataset
                        .promotionLineTotal);
            });
    };

    const apply = async () => {
        const code =
            codeInput.value
                .trim()
                .toUpperCase();

        codeInput.value = code;

        if (!code) {
            currentDiscount = 0;

            setMessage(
                "Nhập mã khuyến mãi để áp dụng.",
                false);

            updateTotals();
            return;
        }

        applyButton.disabled = true;

        setMessage(
            "Đang kiểm tra sản phẩm đủ điều kiện...",
            null);

        try {
            const body =
                new URLSearchParams();

            body.set("code", code);
            appendCartLines(body);

            const response =
                await fetch(endpoint, {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/x-www-form-urlencoded;charset=UTF-8",
                        "RequestVerificationToken":
                            token
                    },
                    body
                });

            const data =
                await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "Mã khuyến mãi không hợp lệ.");
            }

            currentDiscount =
                Number(
                    data.discountAmount || 0);

            setMessage(
                `${data.message} Giá trị hàng đủ điều kiện: ` +
                `${formatMoney(
                    Number(
                        data.eligibleSubtotal || 0))} ${currency}.`,
                true);

            updateTotals();
        }
        catch (error) {
            currentDiscount = 0;

            setMessage(
                error.message ||
                "Không thể kiểm tra mã khuyến mãi.",
                false);

            updateTotals();
        }
        finally {
            applyButton.disabled = false;
        }
    };

    applyButton.addEventListener(
        "click",
        apply);

    codeInput.addEventListener(
        "input",
        () => {
            codeInput.value =
                codeInput.value
                    .toUpperCase()
                    .replace(
                        /[^A-Z0-9-]/g,
                        "");
        });

    form.querySelectorAll(
        "[data-shipping-radio]")
        .forEach(radio =>
            radio.addEventListener(
                "change",
                updateTotals));

    if (codeInput.value.trim()) {
        apply();
    }
    else {
        updateTotals();
    }
})();
