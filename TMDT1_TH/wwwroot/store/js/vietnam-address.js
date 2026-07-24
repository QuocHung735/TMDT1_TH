(() => {
    const API_BASE =
        "https://provinces.open-api.vn/api/v2";

    const CACHE_LIFETIME =
        7 * 24 * 60 * 60 * 1000;

    const collator =
        new Intl.Collator("vi", {
            sensitivity: "base"
        });

    const normalize = value =>
        String(value || "")
            .trim()
            .toLocaleLowerCase("vi");

    const readCache = key => {
        try {
            const raw =
                localStorage.getItem(key);

            if (!raw) return null;

            const parsed = JSON.parse(raw);

            if (!parsed.savedAt ||
                Date.now() - parsed.savedAt >
                CACHE_LIFETIME) {
                localStorage.removeItem(key);
                return null;
            }

            return parsed.data;
        }
        catch {
            return null;
        }
    };

    const writeCache = (key, data) => {
        try {
            localStorage.setItem(
                key,
                JSON.stringify({
                    savedAt: Date.now(),
                    data
                }));
        }
        catch {
            // Trình duyệt có thể chặn localStorage.
        }
    };

    const fetchJson = async url => {
        const controller =
            new AbortController();

        const timeout =
            window.setTimeout(
                () => controller.abort(),
                10000);

        try {
            const response =
                await fetch(url, {
                    headers: {
                        Accept: "application/json"
                    },
                    signal: controller.signal
                });

            if (!response.ok) {
                throw new Error(
                    `HTTP ${response.status}`);
            }

            return await response.json();
        }
        finally {
            window.clearTimeout(timeout);
        }
    };

    const loadProvinces = async () => {
        const key =
            "mayhome.vn-address.v2.provinces";

        const cached = readCache(key);
        if (Array.isArray(cached)) {
            return cached;
        }

        const data =
            await fetchJson(
                `${API_BASE}/p/`);

        const provinces =
            (Array.isArray(data) ? data : [])
                .map(item => ({
                    code: Number(item.code),
                    name: String(item.name || "").trim()
                }))
                .filter(item =>
                    Number.isInteger(item.code) &&
                    item.code > 0 &&
                    item.name)
                .sort((a, b) =>
                    collator.compare(
                        a.name,
                        b.name));

        writeCache(key, provinces);
        return provinces;
    };

    const loadWards = async provinceCode => {
        const key =
            `mayhome.vn-address.v2.wards.${provinceCode}`;

        const cached = readCache(key);
        if (Array.isArray(cached)) {
            return cached;
        }

        const data =
            await fetchJson(
                `${API_BASE}/w/?province=${encodeURIComponent(
                    provinceCode)}`);

        const wards =
            (Array.isArray(data) ? data : [])
                .map(item => ({
                    code: Number(item.code),
                    name: String(item.name || "").trim()
                }))
                .filter(item =>
                    Number.isInteger(item.code) &&
                    item.code > 0 &&
                    item.name)
                .sort((a, b) =>
                    collator.compare(
                        a.name,
                        b.name));

        writeCache(key, wards);
        return wards;
    };

    const addOption = (
        select,
        value,
        text,
        code = "") => {
        const option =
            document.createElement("option");

        option.value = value;
        option.textContent = text;

        if (code !== "") {
            option.dataset.code =
                String(code);
        }

        select.appendChild(option);
        return option;
    };

    const fillSelect = (
        select,
        items,
        placeholder,
        selectedValue,
        legacyLabel) => {
        select.innerHTML = "";
        addOption(
            select,
            "",
            placeholder);

        items.forEach(item => {
            addOption(
                select,
                item.name,
                item.name,
                item.code);
        });

        const expected =
            normalize(selectedValue);

        let matched = false;

        [...select.options]
            .forEach(option => {
                if (expected &&
                    normalize(option.value) ===
                    expected) {
                    option.selected = true;
                    matched = true;
                }
            });

        if (!matched &&
            selectedValue) {
            const legacy =
                addOption(
                    select,
                    selectedValue,
                    `${selectedValue} (${legacyLabel})`);

            legacy.dataset.legacy = "true";
            legacy.selected = true;
        }

        select.disabled = false;
    };

    const injectStyles = () => {
        if (document.getElementById(
                "vn-address-dropdown-styles")) {
            return;
        }

        const style =
            document.createElement("style");

        style.id =
            "vn-address-dropdown-styles";

        style.textContent = `
            .vn-address-status {
                display: flex;
                align-items: center;
                min-height: 38px;
                padding: 9px 12px;
                gap: 8px;
                border-radius: 10px;
                font-size: 10px;
                line-height: 1.45;
            }

            .vn-address-status[hidden] {
                display: none;
            }

            .vn-address-status.is-loading {
                color: #5e5680;
                background: #f5f3ff;
                border: 1px solid #e3defb;
            }

            .vn-address-status.is-error {
                color: #9f3148;
                background: #fff1f4;
                border: 1px solid #f1c9d2;
            }

            .vn-address-status.is-warning {
                color: #795814;
                background: #fff8df;
                border: 1px solid #eadc9c;
            }

            .vn-address-status button {
                padding: 4px 8px;
                color: inherit;
                cursor: pointer;
                background: #fff;
                border: 1px solid currentColor;
                border-radius: 7px;
                font: inherit;
                font-weight: 700;
            }

            select[data-vn-province]:disabled,
            select[data-vn-ward]:disabled {
                cursor: wait;
                opacity: .68;
            }
        `;

        document.head.appendChild(style);
    };

    const initializeForm = form => {
        const provinceSelect =
            form.querySelector(
                "[data-vn-province]");

        const wardSelect =
            form.querySelector(
                "[data-vn-ward]");

        const districtInput =
            form.querySelector(
                "[data-vn-district]");

        const status =
            form.querySelector(
                "[data-vn-address-status]");

        if (!provinceSelect ||
            !wardSelect) {
            return;
        }

        const addressRequired =
            form.dataset.vnAddressRequired ===
            "true";

        const initialProvince =
            provinceSelect.dataset.currentValue
            || provinceSelect.value
            || "";

        const initialWard =
            wardSelect.dataset.currentValue
            || wardSelect.value
            || "";

        let provinces = [];
        let loadingWards = false;

        const setStatus = (
            message,
            type = "",
            retry = null) => {
            if (!status) return;

            status.className =
                `vn-address-status ${type}`
                    .trim();

            status.innerHTML = "";

            if (!message) {
                status.hidden = true;
                return;
            }

            status.hidden = false;

            const text =
                document.createElement("span");

            text.textContent = message;
            status.appendChild(text);

            if (typeof retry === "function") {
                const button =
                    document.createElement(
                        "button");

                button.type = "button";
                button.textContent = "Thử lại";
                button.addEventListener(
                    "click",
                    retry);

                status.appendChild(button);
            }
        };

        const selectedProvinceCode = () => {
            const option =
                provinceSelect
                    .selectedOptions[0];

            const code =
                Number(option?.dataset.code);

            return Number.isInteger(code) &&
                   code > 0
                ? code
                : null;
        };

        const validateAddress = () => {
            provinceSelect.setCustomValidity("");
            wardSelect.setCustomValidity("");

            const hasAnyAddress =
                Boolean(
                    provinceSelect.value ||
                    wardSelect.value ||
                    form.querySelector(
                        "[name='AddressLine']")
                        ?.value?.trim());

            if (addressRequired || hasAnyAddress) {
                if (!provinceSelect.value) {
                    provinceSelect
                        .setCustomValidity(
                            "Vui lòng chọn tỉnh hoặc thành phố.");
                }

                if (!wardSelect.value) {
                    wardSelect
                        .setCustomValidity(
                            "Vui lòng chọn phường, xã hoặc đặc khu.");
                }
            }
        };

        const renderFallback = (
            provinceValue,
            wardValue) => {
            fillSelect(
                provinceSelect,
                [],
                addressRequired
                    ? "Chọn tỉnh/thành phố"
                    : "Chưa chọn",
                provinceValue,
                "địa chỉ cũ");

            fillSelect(
                wardSelect,
                [],
                addressRequired
                    ? "Chọn phường/xã/đặc khu"
                    : "Chưa chọn",
                wardValue,
                "địa chỉ cũ");

            wardSelect.disabled =
                !provinceSelect.value;
        };

        const loadWardOptions = async (
            preferredWard = "") => {
            const provinceCode =
                selectedProvinceCode();

            if (!provinceCode) {
                fillSelect(
                    wardSelect,
                    [],
                    provinceSelect.value
                        ? "Hãy chọn lại tỉnh/thành phố hiện hành"
                        : addressRequired
                            ? "Chọn tỉnh/thành phố trước"
                            : "Chưa chọn",
                    preferredWard,
                    "địa chỉ cũ");

                wardSelect.disabled =
                    !provinceSelect.value;

                return;
            }

            loadingWards = true;
            wardSelect.disabled = true;

            setStatus(
                "Đang tải danh sách phường, xã và đặc khu...",
                "is-loading");

            try {
                const wards =
                    await loadWards(
                        provinceCode);

                fillSelect(
                    wardSelect,
                    wards,
                    addressRequired
                        ? "Chọn phường/xã/đặc khu"
                        : "Chưa chọn",
                    preferredWard,
                    "địa chỉ cũ");

                setStatus("");
            }
            catch {
                fillSelect(
                    wardSelect,
                    [],
                    "Không tải được danh sách",
                    preferredWard,
                    "địa chỉ cũ");

                setStatus(
                    "Không tải được danh sách phường/xã. Kiểm tra kết nối mạng rồi thử lại.",
                    "is-error",
                    () =>
                        loadWardOptions(
                            wardSelect.value ||
                            preferredWard));
            }
            finally {
                loadingWards = false;
                validateAddress();
            }
        };

        const initialize = async () => {
            provinceSelect.disabled = true;
            wardSelect.disabled = true;

            setStatus(
                "Đang tải danh sách tỉnh và thành phố...",
                "is-loading");

            try {
                provinces =
                    await loadProvinces();

                fillSelect(
                    provinceSelect,
                    provinces,
                    addressRequired
                        ? "Chọn tỉnh/thành phố"
                        : "Chưa chọn",
                    initialProvince,
                    "địa chỉ cũ");

                await loadWardOptions(
                    initialWard);

                const selected =
                    provinceSelect
                        .selectedOptions[0];

                if (selected?.dataset.legacy ===
                    "true") {
                    setStatus(
                        "Địa chỉ đang lưu thuộc danh mục cũ. Hãy chọn lại theo danh mục hành chính hiện hành.",
                        "is-warning");
                }
            }
            catch {
                renderFallback(
                    initialProvince,
                    initialWard);

                setStatus(
                    "Không tải được danh mục địa chỉ. Kiểm tra kết nối mạng rồi thử lại.",
                    "is-error",
                    initialize);
            }
        };

        provinceSelect.addEventListener(
            "change",
            async () => {
                if (districtInput) {
                    districtInput.value = "";
                }

                wardSelect.value = "";
                await loadWardOptions("");
                validateAddress();
            });

        wardSelect.addEventListener(
            "change",
            () => {
                if (districtInput) {
                    districtInput.value = "";
                }

                validateAddress();
            });

        form.addEventListener(
            "submit",
            event => {
                validateAddress();

                if (loadingWards) {
                    event.preventDefault();

                    setStatus(
                        "Danh sách địa chỉ vẫn đang được tải. Vui lòng chờ một chút.",
                        "is-loading");

                    return;
                }

                if (!form.checkValidity()) {
                    event.preventDefault();
                    form.reportValidity();
                }
            },
            true);

        initialize();
    };

    injectStyles();

    document
        .querySelectorAll(
            "[data-vn-address-form]")
        .forEach(initializeForm);
})();
