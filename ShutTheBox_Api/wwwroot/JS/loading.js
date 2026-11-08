const minVisibleMs = 300;
const inlineStatuses = new Map();
const allTasks = [];

let toast = null;
let toastTasks = [];
let toastShownAt = 0;
let toastTimer = null;

function isVisible(element) {
    return element.isConnected && !element.closest(".hidden, [hidden], dialog:not([open])");
}

function findTarget(selectors) {
    for (const selector of [].concat(selectors ?? [])) {
        for (const element of document.querySelectorAll(selector)) {
            if (isVisible(element)) {
                return element;
            }
        }
    }
    return null;
}

function createStatusNode(className) {
    const node = document.createElement("span");
    node.className = className;

    const spinner = document.createElement("span");
    spinner.className = "stb-loading-spinner";
    spinner.setAttribute("aria-hidden", "true");

    const text = document.createElement("span");
    text.className = "stb-loading-text";

    node.append(spinner, text);
    return node;
}

function setText(node, text) {
    node.querySelector(".stb-loading-text").textContent = text;
}

function updateBusy() {
    document.body.classList.toggle("stb-busy", allTasks.some(task => task.blocking));
}

function showInline(target, task) {
    let status = inlineStatuses.get(target);
    const beside = /^H[1-6]$/.test(target.tagName);

    if (!status) {
        const className = beside ? "stb-inline-loading stb-inline-loading--beside" : "stb-inline-loading";
        status = { tasks: [], node: createStatusNode(className), original: null, shownAt: 0, timer: null };
        status.node.setAttribute("role", "status");
        status.node.setAttribute("aria-live", "polite");
        inlineStatuses.set(target, status);
    }

    clearTimeout(status.timer);
    status.tasks.push(task);

    if (status.node.parentNode !== target) {
        status.original = document.createDocumentFragment();
        if (!beside) {
            status.original.append(...target.childNodes);
        }
        target.append(status.node);
        status.shownAt = Date.now();
    }

    setText(status.node, task.text);

    return () => {
        status.tasks.splice(status.tasks.indexOf(task), 1);

        if (status.tasks.length > 0) {
            setText(status.node, status.tasks[status.tasks.length - 1].text);
            return;
        }

        const remaining = Math.max(0, minVisibleMs - (Date.now() - status.shownAt));
        status.timer = setTimeout(() => {
            if (status.tasks.length > 0) {
                return;
            }
            inlineStatuses.delete(target);
            if (status.node.parentNode === target) {
                status.node.replaceWith(status.original);
            }
        }, remaining);
    };
}

function getToast() {
    if (!toast) {
        toast = createStatusNode("stb-loading");
        toast.setAttribute("role", "status");
        toast.setAttribute("aria-live", "polite");
        toast.setAttribute("popover", "manual");
        document.body.append(toast);
    }
    return toast;
}

function updateToast() {
    const el = getToast();
    clearTimeout(toastTimer);

    if (toastTasks.length > 0) {
        if (!el.classList.contains("stb-loading--visible")) {
            toastShownAt = Date.now();
        }
        setText(el, toastTasks[toastTasks.length - 1].text);
        if (typeof el.showPopover === "function") {
            if (el.classList.contains("stb-loading--visible")) {
                el.hidePopover();
            }
            el.showPopover();
        }
        el.classList.add("stb-loading--visible");
        return;
    }

    const remaining = Math.max(0, minVisibleMs - (Date.now() - toastShownAt));
    toastTimer = setTimeout(() => {
        if (el.classList.contains("stb-loading--visible") && typeof el.hidePopover === "function") {
            el.hidePopover();
        }
        el.classList.remove("stb-loading--visible");
    }, remaining);
}

function showToast(task) {
    toastTasks.push(task);
    updateToast();

    return () => {
        toastTasks.splice(toastTasks.indexOf(task), 1);
        updateToast();
    };
}

export function startLoading(text, { blocking = false, target = null } = {}) {
    const task = { text, blocking };
    allTasks.push(task);
    updateBusy();

    const element = findTarget(target);
    const hide = element ? showInline(element, task) : showToast(task);

    let stopped = false;
    return function stopLoading() {
        if (stopped) {
            return;
        }
        stopped = true;
        allTasks.splice(allTasks.indexOf(task), 1);
        updateBusy();
        hide();
    };
}

export async function withLoading(text, action, options) {
    const stopLoading = startLoading(text, options);
    try {
        return await action();
    }
    finally {
        stopLoading();
    }
}
