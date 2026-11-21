(() => {
    "use strict";

    const map = document.getElementById("tableMap");
    if (!map) return;

    const wrapper = map.closest(".table-map-wrapper");
    const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    const MAP_WIDTH = 1200;
    const MAP_HEIGHT = 700;
    const MIN_OBJECT_SIZE = 2;
    const MAX_OBJECT_SIZE = 100;

    const elements = {
        edit: document.getElementById("btnEditLayout"),
        save: document.getElementById("btnSaveLayout"),
        cancel: document.getElementById("btnCancelLayout"),
        zoomIn: document.getElementById("btnZoomIn"),
        zoomOut: document.getElementById("btnZoomOut"),
        zoomFit: document.getElementById("btnZoomFit"),
        zoomLabel: document.getElementById("zoomLevel"),
        message: document.getElementById("mapMessage"),

        objectPanel: document.getElementById("mapObjectProperties"),
        objectTitle: document.getElementById("mapObjectTitle"),
        objectX: document.getElementById("objectX"),
        objectY: document.getElementById("objectY"),
        objectWidth: document.getElementById("objectWidth"),
        objectHeight: document.getElementById("objectHeight"),
        objectRotation: document.getElementById("objectRotation"),

        deleteObject: document.getElementById("btnDeleteMapObject"),
        resetObjects: document.getElementById("btnResetMapObjects"),

        tableInfo: document.getElementById("tableInfo"),
        selectedTableName: document.getElementById("selectedTableName"),
        selectedTableCapacity: document.getElementById("selectedTableCapacity"),
        selectedTableStatus: document.getElementById("selectedTableStatus"),
        openTable: document.getElementById("btnOpenSelectedTable")
    };

    map.querySelectorAll(".map-table").forEach(table => {
        table.style.left = `${table.dataset.x}%`;
        table.style.top = `${table.dataset.y}%`;
    });

    let editing = false;
    let selectedObject = null;
    let selectedTable = null;

    let zoom = 1;
    let fitScale = 1;

    let objectItems = [];
    let originalTables = [];
    let originalObjects = [];

    let pointerOperation = null;
    let objectIdCounter = 0;

    const objectDefaults = {
        counter: {
            label: "Quầy thu ngân",
            width: 18,
            height: 10
        },
        window: {
            label: "Cửa sổ",
            width: 14,
            height: 4
        },
        door: {
            label: "Cửa ra vào",
            width: 9,
            height: 5
        },
        partition: {
            label: "Vách ngăn",
            width: 18,
            height: 3
        },
        plant: {
            label: "Chậu cây",
            width: 6,
            height: 8
        },
        decoration: {
            label: "Trang trí",
            width: 9,
            height: 8
        }
    };

    const clone = value => JSON.parse(JSON.stringify(value));
    const GRID_SIZE = 25;



    function snapPercent(value, totalSize) {
        const pixelPosition = (value / 100) * totalSize;
        const snappedPosition =
            Math.round(pixelPosition / GRID_SIZE) * GRID_SIZE;

        return clamp((snappedPosition / totalSize) * 100, 0, 100);
    }
    function showMessage(message, isError = false) {
        if (!elements.message) return;

        elements.message.textContent = message;
        elements.message.className =
            "small mt-2 " + (isError ? "text-danger" : "text-success");
    }

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function getFloorId() {
        const selector = document.querySelector('select[name="maTang"]');
        return selector?.value || map.dataset.floorId || "";
    }

    function getMapPoint(event) {
        const rect = map.getBoundingClientRect();

        return {
            x: clamp(
                ((event.clientX - rect.left) / rect.width) * 100,
                0,
                100
            ),
            y: clamp(
                ((event.clientY - rect.top) / rect.height) * 100,
                0,
                100
            )
        };
    }

    function applyZoom() {
        if (!wrapper) return;

        const availableWidth = Math.max(wrapper.clientWidth - 24, 280);

        fitScale = Math.min(1, availableWidth / MAP_WIDTH);
        const finalZoom = fitScale * zoom;

        map.style.zoom = finalZoom;

        if (elements.zoomLabel) {
            elements.zoomLabel.textContent =
                `${Math.round(zoom * 100)}%`;
        }
    }

    function fitMap() {
        zoom = 1;
        applyZoom();
    }

    function setEditing(value) {
        editing = value;
        map.classList.toggle("editing", editing);

        if (elements.edit) {
            elements.edit.disabled = editing;
        }

        if (elements.save) {
            elements.save.disabled = !editing;
            elements.save.style.display = editing ? "" : "none";
        }

        if (elements.cancel) {
            elements.cancel.disabled = !editing;
            elements.cancel.style.display = editing ? "" : "none";
        }

        document.querySelectorAll("[data-add-object]").forEach(button => {
            button.disabled = !editing;
        });

        if (!editing) {
            clearSelection();
        }
    }

    function captureOriginalState() {
        originalTables = [...map.querySelectorAll(".map-table")].map(table => ({
            element: table,
            x: Number(table.dataset.x),
            y: Number(table.dataset.y)
        }));

        originalObjects = clone(objectItems);
    }

    function restoreOriginalState() {
        originalTables.forEach(item => {
            item.element.dataset.x = item.x;
            item.element.dataset.y = item.y;
            item.element.style.left = `${item.x}%`;
            item.element.style.top = `${item.y}%`;
        });

        objectItems = clone(originalObjects);
        renderObjects();
        clearSelection();
    }

    function clearSelection() {
        if (selectedObject) {
            selectedObject.classList.remove("is-selected");
        }

        if (selectedTable) {
            selectedTable.classList.remove("is-selected");
        }

        selectedObject = null;
        selectedTable = null;

        pointerOperation = null;

        if (elements.objectPanel) {
            elements.objectPanel.hidden = true;
        }

        if (elements.deleteObject) {
            elements.deleteObject.disabled = true;
        }

        if (elements.tableInfo) {
            elements.tableInfo.hidden = true;
        }
    }

    function selectTable(table) {
        clearSelection();

        selectedTable = table;
        table.classList.add("is-selected");

        if (elements.tableInfo) {
            elements.tableInfo.hidden = false;
        }

        if (elements.selectedTableName) {
            elements.selectedTableName.textContent =
                table.dataset.name || "Bàn";
        }

        if (elements.selectedTableCapacity) {
            elements.selectedTableCapacity.textContent =
                table.dataset.capacity || "—";
        }

        if (elements.selectedTableStatus) {
            elements.selectedTableStatus.textContent =
                table.dataset.status || "—";
        }

        if (elements.openTable) {
            const baseUrl = map.dataset.openTableUrl;

            if (baseUrl) {
                elements.openTable.href =
                    `${baseUrl}?id=${encodeURIComponent(table.dataset.id)}`;
            }
        }
    }

    function createObject(type) {
        const defaults = objectDefaults[type];
        if (!defaults) return;

        const item = {
            id: `map-object-${Date.now()}-${++objectIdCounter}`,
            type,
            label: defaults.label,
            x: snapPercent(50, MAP_WIDTH),
            y: snapPercent(50, MAP_HEIGHT),
            width: defaults.width,
            height: defaults.height,
            rotation: 0
        };

        objectItems.push(item);
        renderObjects();
        selectObject(item.id);
        showMessage(`Đã thêm ${defaults.label}.`);
    }

    function getObjectElement(id) {
        return map.querySelector(
            `.map-object[data-object-id="${CSS.escape(id)}"]`
        );
    }

    function renderObjects() {
        map.querySelectorAll(".map-object").forEach(element => {
            element.remove();
        });

        objectItems.forEach(item => {
            const element = document.createElement("div");

            element.className = `map-object map-object-${item.type}`;
            element.dataset.objectId = item.id;
            element.dataset.type = item.type;

            element.style.left = `${item.x}%`;
            element.style.top = `${item.y}%`;
            element.style.width = `${item.width}%`;
            element.style.height = `${item.height}%`;
            element.style.transform = `translate(-50%, -50%) rotate(${item.rotation}deg)`;

            const label = document.createElement("span");
            label.className = "map-object-label";
            label.textContent = item.label || objectDefaults[item.type].label;
            element.appendChild(label);

            if (editing) {
                const resize = document.createElement("span");
                resize.className = "map-object-handle resize";
                resize.dataset.handle = "resize";
                element.appendChild(resize);

                const rotate = document.createElement("span");
                rotate.className = "map-object-handle rotate";
                rotate.dataset.handle = "rotate";
                element.appendChild(rotate);
            }

            map.appendChild(element);
        });

        if (selectedObject) {
            selectedObject = getObjectElement(
                selectedObject.dataset.objectId
            );

            selectedObject?.classList.add("is-selected");
        }
    }

    function selectObject(id) {
        clearSelection();

        const element = getObjectElement(id);
        const item = objectItems.find(object => object.id === id);

        if (!element || !item) return;

        selectedObject = element;
        selectedObject.classList.add("is-selected");

        if (elements.objectPanel) {
            elements.objectPanel.hidden = false;
        }

        if (elements.deleteObject) {
            elements.deleteObject.disabled = !editing;
        }

        if (elements.objectTitle) {
            elements.objectTitle.textContent =
                item.label || objectDefaults[item.type].label;
        }

        updatePropertyInputs(item);
    }

    function updatePropertyInputs(item) {
        if (elements.objectX) elements.objectX.value = item.x;
        if (elements.objectY) elements.objectY.value = item.y;
        if (elements.objectWidth) elements.objectWidth.value = item.width;
        if (elements.objectHeight) elements.objectHeight.value = item.height;
        if (elements.objectRotation) {
            elements.objectRotation.value = item.rotation;
        }
    }

    function updateSelectedObject(property, value) {
        if (!selectedObject || !editing) return;

        const item = objectItems.find(
            object => object.id === selectedObject.dataset.objectId
        );

        if (!item) return;

        const limits = {
            x: [0, 100],
            y: [0, 100],
            width: [MIN_OBJECT_SIZE, MAX_OBJECT_SIZE],
            height: [MIN_OBJECT_SIZE, MAX_OBJECT_SIZE],
            rotation: [0, 359]
        };

        if (!limits[property]) return;

        const numericValue = Number(value);
        if (!Number.isFinite(numericValue)) return;

        item[property] = clamp(
            numericValue,
            limits[property][0],
            limits[property][1]
        );

        if (property === "x") {
            item.x = snapPercent(item.x, MAP_WIDTH);
        }

        if (property === "y") {
            item.y = snapPercent(item.y, MAP_HEIGHT);
        }

        if (property === "width") {
            item.width = Math.max(
                GRID_SIZE / MAP_WIDTH * 100,
                Math.round(item.width / 100 * MAP_WIDTH / GRID_SIZE)
                * GRID_SIZE / MAP_WIDTH * 100
            );
        }

        if (property === "height") {
            item.height = Math.max(
                GRID_SIZE / MAP_HEIGHT * 100,
                Math.round(item.height / 100 * MAP_HEIGHT / GRID_SIZE)
                * GRID_SIZE / MAP_HEIGHT * 100
            );
        }

        applyObjectStyle(selectedObject, item);
    }

    function applyObjectStyle(element, item) {
        element.style.left = `${item.x}%`;
        element.style.top = `${item.y}%`;
        element.style.width = `${item.width}%`;
        element.style.height = `${item.height}%`;
        element.style.transform =
            `translate(-50%, -50%) rotate(${item.rotation}deg)`;

        const label = element.querySelector(".map-object-label");
        if (label) label.textContent = item.label;
    }

    function deleteSelectedObject() {
        if (!editing || !selectedObject) return;

        const id = selectedObject.dataset.objectId;

        objectItems = objectItems.filter(item => item.id !== id);
        selectedObject.remove();

        clearSelection();
        showMessage("Đã xóa đối tượng.");
    }

    function startPointerOperation(event, element, operation) {
        if (!editing) return;

        event.preventDefault();
        event.stopPropagation();

        const point = getMapPoint(event);
        const item = objectItems.find(
            object => object.id === element.dataset.objectId
        );

        if (!item) return;

        selectObject(item.id);

        pointerOperation = {
            pointerId: event.pointerId,
            element,
            item,
            operation,
            startX: point.x,
            startY: point.y,
            initialX: item.x,
            initialY: item.y,
            initialWidth: item.width,
            initialHeight: item.height,
            initialRotation: item.rotation
        };

        element.setPointerCapture(event.pointerId);
    }

    function onObjectPointerMove(event) {
        const operation = pointerOperation;
        if (!operation || operation.pointerId !== event.pointerId) return;

        const point = getMapPoint(event);
        const dx = point.x - operation.startX;
        const dy = point.y - operation.startY;
        const item = operation.item;

        if (operation.operation === "move") {
            item.x = clamp(operation.initialX + dx, 0, 100);
            item.y = clamp(operation.initialY + dy, 0, 100);
        }

        if (operation.operation === "resize") {
            item.width = clamp(
                operation.initialWidth + dx,
                MIN_OBJECT_SIZE,
                MAX_OBJECT_SIZE
            );

            item.height = clamp(
                operation.initialHeight + dy,
                MIN_OBJECT_SIZE,
                MAX_OBJECT_SIZE
            );
        }

        if (operation.operation === "rotate") {
            const rect = map.getBoundingClientRect();
            const centerX =
                rect.left + (item.x / 100) * rect.width;
            const centerY =
                rect.top + (item.y / 100) * rect.height;

            const angle = Math.atan2(
                event.clientY - centerY,
                event.clientX - centerX
            ) * (180 / Math.PI);

            item.rotation = (Math.round(angle) + 360) % 360;
        }

        applyObjectStyle(operation.element, item);
        updatePropertyInputs(item);
    }

    function finishPointerOperation(event) {
        if (!pointerOperation) return;

        if (
            event &&
            pointerOperation.pointerId !== event.pointerId
        ) {
            return;
        }

        const operation = pointerOperation;

        if (operation.operation === "move") {
            operation.item.x = snapPercent(
                operation.item.x,
                MAP_WIDTH
            );

            operation.item.y = snapPercent(
                operation.item.y,
                MAP_HEIGHT
            );
        }

        if (operation.operation === "resize") {
            operation.item.width = Math.max(
                GRID_SIZE / MAP_WIDTH * 100,
                Math.round(
                    (operation.item.width / 100 * MAP_WIDTH) / GRID_SIZE
                ) * GRID_SIZE / MAP_WIDTH * 100
            );

            operation.item.height = Math.max(
                GRID_SIZE / MAP_HEIGHT * 100,
                Math.round(
                    (operation.item.height / 100 * MAP_HEIGHT) / GRID_SIZE
                ) * GRID_SIZE / MAP_HEIGHT * 100
            );
        }

        applyObjectStyle(operation.element, operation.item);
        updatePropertyInputs(operation.item);

        pointerOperation = null;
    }

    function startTableDrag(event, table) {
        if (!editing || event.button !== 0) return;

        event.preventDefault();
        event.stopPropagation();

        clearSelection();

        const point = getMapPoint(event);

        pointerOperation = {
            pointerId: event.pointerId,
            element: table,
            operation: "table",
            startX: point.x,
            startY: point.y,
            initialX: Number(table.dataset.x),
            initialY: Number(table.dataset.y)
        };

        table.setPointerCapture(event.pointerId);
        table.classList.add("is-dragging");
    }

    function onTablePointerMove(event) {
        const operation = pointerOperation;

        if (
            !operation ||
            operation.operation !== "table" ||
            operation.pointerId !== event.pointerId
        ) {
            return;
        }

        const point = getMapPoint(event);
        const dx = point.x - operation.startX;
        const dy = point.y - operation.startY;

        const x = clamp(operation.initialX + dx, 4, 96);
        const y = clamp(operation.initialY + dy, 5, 95);

        operation.element.dataset.x = x;
        operation.element.dataset.y = y;
        operation.element.style.left = `${x}%`;
        operation.element.style.top = `${y}%`;
    }

    function finishTableDrag(event) {
        if (
            !pointerOperation ||
            pointerOperation.operation !== "table" ||
            pointerOperation.pointerId !== event.pointerId
        ) {
            return;
        }

        const table = pointerOperation.element;

        const x = snapPercent(
            Number(table.dataset.x),
            MAP_WIDTH
        );

        const y = snapPercent(
            Number(table.dataset.y),
            MAP_HEIGHT
        );

        table.dataset.x = x;
        table.dataset.y = y;

        table.style.left = `${x}%`;
        table.style.top = `${y}%`;

        table.classList.remove("is-dragging");

        pointerOperation = null;
    }

    async function loadObjects() {
        const url = map.dataset.loadObjectsUrl;
        if (!url) return;

        try {
            const response = await fetch(
                `${url}?maTang=${encodeURIComponent(getFloorId())}`,
                {
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                }
            );

            if (!response.ok) {
                throw new Error("Không thể tải nội thất.");
            }

            const data = await response.json();

            if (Array.isArray(data)) {
                objectItems = data;
                renderObjects();
            }
        } catch (error) {
            console.error(error);
            showMessage(
                "Không tải được nội thất từ máy chủ.",
                true
            );
        }
    }

    async function saveObjects() {
        const url = map.dataset.saveObjectsUrl;
        if (!url) {
            throw new Error("Chưa cấu hình URL lưu nội thất.");
        }

        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token || ""
            },
            body: JSON.stringify({
                maTang: Number(getFloorId()),
                objects: objectItems
            })
        });

        if (!response.ok) {
            throw new Error("Lưu nội thất thất bại.");
        }

        const result = await response.json();

        if (result.success === false) {
            throw new Error(result.message || "Lưu nội thất thất bại.");
        }
    }


    async function saveTablePositions() {
        const url = map.dataset.savePositionsUrl;
        if (!url) {
            throw new Error("Chưa cấu hình URL lưu vị trí bàn.");
        }

        const positions = [...map.querySelectorAll(".map-table")].map(table => ({
            maBan: Number(table.dataset.id),
            viTriX: Math.round(Number(table.dataset.x)),
            viTriY: Math.round(Number(table.dataset.y))
        }));

        const separator = url.includes("?") ? "&" : "?";

        const response = await fetch(
            `${url}${separator}maTang=${encodeURIComponent(getFloorId())}`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token || ""
                },
                body: JSON.stringify(positions)
            }
        );

        if (!response.ok) {
            throw new Error("Không thể lưu vị trí bàn.");
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || "Lưu vị trí bàn thất bại.");
        }
    }


    async function saveLayout() {
        if (!editing) return;

        if (elements.save) {
            elements.save.disabled = true;
            elements.save.textContent = "Đang lưu...";
        }

        try {
            await saveTablePositions();
            await saveObjects();

            captureOriginalState();
            setEditing(false);
            showMessage("Đã lưu sơ đồ thành công.");
        } catch (error) {
            console.error(error);
            showMessage(error.message, true);
        } finally {
            if (elements.save) {
                elements.save.disabled = !editing;
                elements.save.textContent = "Lưu vị trí";
            }
        }
    }

    function resetObjects() {
        if (!editing) return;

        if (!confirm("Xóa toàn bộ nội thất và vật dụng trên tầng này?")) {
            return;
        }

        objectItems = [];
        renderObjects();
        clearSelection();
        showMessage("Đã xóa nội thất khỏi sơ đồ. Nhấn Lưu để xác nhận.");
    }

    // ---------------------------------------------------------
    // Pointer events
    // ---------------------------------------------------------

    map.addEventListener("pointerdown", event => {
        const table = event.target.closest(".map-table");

        if (table) {
            if (editing) {
                startTableDrag(event, table);
            } else {
                selectTable(table);
            }

            return;
        }

        const object = event.target.closest(".map-object");

        if (object) {
            if (!editing) return;

            const handle = event.target.closest("[data-handle]");

            const operation = handle
                ? handle.dataset.handle
                : "move";

            startPointerOperation(event, object, operation);
            return;
        }

        if (event.target === map || event.target === map.querySelector("#mapEnvironment")) {
            clearSelection();
        }
    });

    map.addEventListener("pointermove", event => {
        if (pointerOperation?.operation === "table") {
            onTablePointerMove(event);
        } else {
            onObjectPointerMove(event);
        }
    });

    map.addEventListener("pointerup", event => {
        if (pointerOperation?.operation === "table") {
            finishTableDrag(event);
        } else {
            finishPointerOperation(event);
        }
    });

    map.addEventListener("pointercancel", event => {
        if (pointerOperation?.operation === "table") {
            finishTableDrag(event);
        } else {
            finishPointerOperation(event);
        }
    });

    // ---------------------------------------------------------
    // Toolbar
    // ---------------------------------------------------------

    elements.edit?.addEventListener("click", () => {
        captureOriginalState();
        setEditing(true);
        showMessage("Chế độ chỉnh sửa đã bật.");
    });

    elements.save?.addEventListener("click", saveLayout);

    elements.cancel?.addEventListener("click", () => {
        restoreOriginalState();
        setEditing(false);
        showMessage("Đã hủy các thay đổi chưa lưu.");
    });

    document.querySelectorAll("[data-add-object]").forEach(button => {
        button.addEventListener("click", () => {
            createObject(button.dataset.addObject);
        });
    });

    elements.deleteObject?.addEventListener(
        "click",
        deleteSelectedObject
    );

    elements.resetObjects?.addEventListener(
        "click",
        resetObjects
    );

    // ---------------------------------------------------------
    // Property editor
    // ---------------------------------------------------------

    const propertyInputs = [
        ["objectX", "x"],
        ["objectY", "y"],
        ["objectWidth", "width"],
        ["objectHeight", "height"],
        ["objectRotation", "rotation"]
    ];

    propertyInputs.forEach(([id, property]) => {
        const input = document.getElementById(id);

        input?.addEventListener("change", () => {
            updateSelectedObject(property, input.value);
        });
    });

    // ---------------------------------------------------------
    // Zoom and responsive sizing
    // ---------------------------------------------------------

    elements.zoomIn?.addEventListener("click", () => {
        zoom = clamp(zoom + 0.1, 0.5, 1.8);
        applyZoom();
    });

    elements.zoomOut?.addEventListener("click", () => {
        zoom = clamp(zoom - 0.1, 0.5, 1.8);
        applyZoom();
    });

    elements.zoomFit?.addEventListener("click", fitMap);

    if (wrapper && "ResizeObserver" in window) {
        const observer = new ResizeObserver(applyZoom);
        observer.observe(wrapper);
    } else {
        window.addEventListener("resize", applyZoom);
    }

    // ---------------------------------------------------------
    // Floor changes
    // ---------------------------------------------------------

    document.querySelector('select[name="maTang"]')
        ?.addEventListener("change", () => {
            // The existing floor selector submits the GET form.
            // No client-side floor switching is needed here.
        });

    // Initial setup
    setEditing(false);
    applyZoom();
    captureOriginalState();
    loadObjects();
})();
