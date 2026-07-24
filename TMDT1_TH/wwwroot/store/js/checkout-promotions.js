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

    let appliedCode = null;

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
        discountOutput.dataset.discount =
            String(currentDiscount);

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

    const setDiscount = value => {
        const parsed = Number(value || 0);

        currentDiscount =
            Number.isFinite(parsed) &&
            parsed > 0
                ? parsed
                : 0;

        updateTotals();
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

    const readResponse = async response => {
        const contentType =
            response.headers.get(
                "content-type") || "";

        if (contentType.includes(
                "application/json")) {
            return await response.json();
        }

        return {
            message:
                response.status === 401 ||
                response.status === 403
                    ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
                    : "Không thể kiểm tra mã khuyến mãi lúc này."
        };
    };

    const apply = async () => {
        const code =
            codeInput.value
                .trim()
                .toUpperCase();

        codeInput.value = code;

        if (!code) {
            appliedCode = null;
            setDiscount(0);

            setMessage(
                "Nhập mã khuyến mãi để áp dụng.",
                false);

            return;
        }

        applyButton.disabled = true;

        setMessage(
            "Đang kiểm tra giỏ hàng và sản phẩm đủ điều kiện...",
            null);

        try {
            const body =
                new URLSearchParams();

            body.set("code", code);

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
                await readResponse(response);

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "Mã khuyến mãi không hợp lệ.");
            }

            appliedCode = code;
            setDiscount(
                data.discountAmount || 0);

            setMessage(
                `${data.message} Giá trị hàng đủ điều kiện: ` +
                `${formatMoney(
                    Number(
                        data.eligibleSubtotal || 0))} ${currency}.`,
                true);
        }
        catch (error) {
            appliedCode = null;
            setDiscount(0);

            setMessage(
                error.message ||
                "Không thể kiểm tra mã khuyến mãi.",
                false);
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

            if (appliedCode !== null &&
                codeInput.value !== appliedCode) {
                appliedCode = null;
                setDiscount(0);

                setMessage(
                    codeInput.value
                        ? "Mã đã thay đổi. Bấm Áp dụng để kiểm tra lại."
                        : "Đã bỏ mã khuyến mãi.",
                    null);
            }
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
        setDiscount(0);
    }
})();
