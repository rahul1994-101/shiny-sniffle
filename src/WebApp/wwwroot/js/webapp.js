window.webAppTheme = (function () {
  var storageKey = "app-theme";

  function normalize(theme) {
    return theme === "light" ? "light" : "dark";
  }

  function get() {
    return normalize(document.documentElement.getAttribute("data-theme"));
  }

  function syncToggleUi(theme) {
    var isDark = theme === "dark";
    document.querySelectorAll("[data-theme-toggle]").forEach(function (btn) {
      btn.setAttribute("aria-label", isDark ? "Switch to light mode" : "Switch to dark mode");
      btn.title = isDark ? "Light mode" : "Dark mode";
    });
  }

  function set(theme) {
    var next = normalize(theme);
    document.documentElement.setAttribute("data-theme", next);
    try {
      localStorage.setItem(storageKey, next);
    } catch (e) {
      /* private mode */
    }
    syncToggleUi(next);
    document.dispatchEvent(
      new CustomEvent("app-theme-changed", { detail: { theme: next } })
    );
  }

  function toggle() {
    set(get() === "dark" ? "light" : "dark");
  }

  function init() {
    var stored = null;
    try {
      stored = localStorage.getItem(storageKey);
    } catch (e) {
      stored = null;
    }
    set(stored === "light" ? "light" : "dark");

    document.addEventListener("click", function (e) {
      if (e.target.closest("[data-theme-toggle]")) {
        e.preventDefault();
        toggle();
      }
    });
  }

  return { init: init, get: get, set: set, toggle: toggle };
})();

window.getAppTheme = function () {
  return window.webAppTheme.get();
};

window.setAppTheme = function (theme) {
  window.webAppTheme.set(theme);
};

(function () {
  var themeChangeSubs = new Map();
  var themeChangeSubId = 0;

  window.subscribeAppThemeChanged = function (dotNetRef) {
    var id = ++themeChangeSubId;
    var handler = function (e) {
      var theme = e.detail && e.detail.theme;
      if (theme) {
        dotNetRef.invokeMethodAsync("OnAppThemeChanged", theme);
      }
    };
    themeChangeSubs.set(id, handler);
    document.addEventListener("app-theme-changed", handler);
    return id;
  };

  window.unsubscribeAppThemeChanged = function (id) {
    var handler = themeChangeSubs.get(id);
    if (!handler) return;
    document.removeEventListener("app-theme-changed", handler);
    themeChangeSubs.delete(id);
  };
})();

window.webAppShell = (function () {
  var mq = window.matchMedia ? window.matchMedia("(max-width: 640px)") : null;
  var shellId = "app-shell";
  var delegationReady = false;
  var viewportListenerReady = false;
  /** @type {boolean | null} null = use viewport default on first init */
  var sidebarCollapsedPreference = null;

  function getShell() {
    return document.getElementById(shellId);
  }

  function setSidebarClass(shell, collapsed) {
    if (!shell) return;
    if (collapsed) shell.classList.add("sidebar-collapsed");
    else shell.classList.remove("sidebar-collapsed");
  }

  function isCollapsed(shell) {
    return shell.classList.contains("sidebar-collapsed");
  }

  function syncShellUi() {
    var shell = getShell();
    if (!shell) return;
    var collapsed = isCollapsed(shell);
    var mobile = mq && mq.matches;

    shell.querySelectorAll("[data-shell-toggle]").forEach(function (b) {
      b.setAttribute("aria-expanded", (!collapsed).toString());
      b.title = collapsed ? "Show sidebar" : "Hide sidebar";
    });

    var bd = document.getElementById("sidebar-backdrop");
    if (bd) {
      if (mobile && !collapsed) {
        bd.hidden = false;
        bd.setAttribute("aria-hidden", "false");
      } else {
        bd.hidden = true;
        bd.setAttribute("aria-hidden", "true");
      }
    }

    document.body.classList.toggle("shell-sidebar-drawer-open", mobile && !collapsed);
  }

  function applySidebarState(collapsed) {
    sidebarCollapsedPreference = collapsed;
    setSidebarClass(getShell(), collapsed);
    syncShellUi();
  }

  function setSidebarCollapsed(collapsed) {
    applySidebarState(collapsed);
  }

  function collapseSidebar() {
    applySidebarState(true);
  }

  function collapseSidebarIfMobile() {
    if (mq && mq.matches) {
      collapseSidebar();
    }
  }

  function applyViewportDefault() {
    var shell = getShell();
    if (!shell) return;

    if (sidebarCollapsedPreference === null) {
      sidebarCollapsedPreference = !!(mq && mq.matches);
    }

    setSidebarClass(shell, sidebarCollapsedPreference);
    syncShellUi();
  }

  function restoreSidebarState() {
    if (sidebarCollapsedPreference === null) return;
    setSidebarClass(getShell(), sidebarCollapsedPreference);
    syncShellUi();
  }

  function setupDelegation() {
    if (delegationReady) return;
    delegationReady = true;

    document.addEventListener(
      "click",
      function (e) {
        var shell = getShell();
        if (!shell) return;

        var t = e.target;
        var el = t && t.nodeType === 1 ? t : t && t.parentElement;
        if (!el || typeof el.closest !== "function") return;
        if (!shell.contains(el)) return;

        var closeEl = el.closest("[data-shell-close]");
        if (closeEl && shell.contains(closeEl)) {
          e.preventDefault();
          collapseSidebar();
          return;
        }

        var toggleEl = el.closest("[data-shell-toggle]");
        if (toggleEl && shell.contains(toggleEl)) {
          e.preventDefault();
          applySidebarState(!isCollapsed(shell));
        }
      },
      true
    );

    document.addEventListener("keydown", function (e) {
      if (e.key !== "Escape") return;
      if (!mq || !mq.matches) return;
      var shell = getShell();
      if (!shell || isCollapsed(shell)) return;
      collapseSidebar();
    });
  }

  function setupViewportListener() {
    if (viewportListenerReady || !mq) return;
    viewportListenerReady = true;

    var onMedia = function () {
      sidebarCollapsedPreference = !!(mq && mq.matches);
      applyViewportDefault();
    };

    if (typeof mq.addEventListener === "function") {
      mq.addEventListener("change", onMedia);
    } else if (typeof mq.addListener === "function") {
      mq.addListener(onMedia);
    }
  }

  function initShell() {
    setupDelegation();
    setupViewportListener();
    applyViewportDefault();
  }

  return {
    setSidebarCollapsed: setSidebarCollapsed,
    syncShellUi: syncShellUi,
    restoreSidebarState: restoreSidebarState,
    initShell: initShell,
    collapseSidebar: collapseSidebar,
    collapseSidebarIfMobile: collapseSidebarIfMobile
  };
})();

(function () {
  function tryInitTheme() {
    if (window.webAppTheme) {
      window.webAppTheme.init();
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", tryInitTheme);
  } else {
    tryInitTheme();
  }
})();

(function () {
  var shellReady = false;

  function tryInit() {
    if (!document.getElementById("app-shell")) return;
    if (!shellReady) {
      window.webAppShell.initShell();
      shellReady = true;
      return;
    }
    window.webAppShell.restoreSidebarState();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", tryInit);
  } else {
    tryInit();
  }

  var obs = new MutationObserver(tryInit);
  obs.observe(document.documentElement, { childList: true, subtree: true });
})();

window.webAppScroll = {
  scrollElementToEnd: function (el) {
    if (!el) return;
    requestAnimationFrame(function () {
      el.scrollTop = el.scrollHeight;
    });
  }
};

window.webAppChat = {
  resizeTextarea: function (el, maxLines) {
    if (!el) return;
    var lines = maxLines || 3;
    var style = window.getComputedStyle(el);
    var lineHeight = parseFloat(style.lineHeight);
    if (!lineHeight || isNaN(lineHeight)) {
      lineHeight = parseFloat(style.fontSize) * 1.45;
    }
    var verticalPad =
      (parseFloat(style.paddingTop) || 0) + (parseFloat(style.paddingBottom) || 0);
    var verticalBorder =
      (parseFloat(style.borderTopWidth) || 0) +
      (parseFloat(style.borderBottomWidth) || 0);
    var maxHeight = Math.ceil(lineHeight * lines + verticalPad + verticalBorder);

    el.style.overflowY = "hidden";
    el.style.height = "auto";
    var contentHeight = el.scrollHeight;
    var next = Math.min(contentHeight, maxHeight);
    el.style.height = next + "px";

    var scrollable = contentHeight > maxHeight;
    el.classList.toggle("chat-input-scrollable", scrollable);
    el.style.overflowY = scrollable ? "auto" : "hidden";
  }
};

window.webAppLogin = {
  wireForm: function (formOrId) {
    var form =
      typeof formOrId === "string" ? document.getElementById(formOrId) : formOrId;
    if (!form || form.dataset.loginWired === "1") return;
    form.dataset.loginWired = "1";

    document.body.classList.add("app-login-page");

    form.addEventListener("submit", function () {
      document.body.classList.add("login-pending-navigation");
      form.classList.add("is-submitting");
      form.setAttribute("aria-busy", "true");

      var overlay = document.getElementById("login-loading");
      if (overlay) {
        overlay.hidden = false;
        overlay.setAttribute("aria-hidden", "false");
      }

      var submit = form.querySelector(".login-submit");
      if (submit) {
        submit.disabled = true;
        submit.setAttribute("aria-busy", "true");
      }

      form.querySelectorAll(".login-input").forEach(function (input) {
        input.readOnly = true;
      });
    });
  }
};

(function () {
  function tryInitLogin() {
    var form = document.getElementById("login-form");
    if (!form) return;
    window.webAppLogin.wireForm(form);
  }

  var loginObs = new MutationObserver(tryInitLogin);
  loginObs.observe(document.documentElement, { childList: true, subtree: true });
  tryInitLogin();
})();

window.settingsEditorDialog = {
  syncOpen: function (el, open) {
    if (!el || typeof el.showModal !== "function") return;
    if (open && !el.open) el.showModal();
    else if (!open && el.open) el.close();
  },
  closeOpenDialogs: function () {
    document.querySelectorAll("dialog.settings-editor-dialog").forEach(function (el) {
      if (el.open && typeof el.close === "function") {
        el.close();
      }
    });
  },
  getLayoutPreference: function (pageKey) {
    try {
      var storageKey = pageKey
        ? "settings-editor-layout:" + pageKey
        : "settings-editor-layout";
      return sessionStorage.getItem(storageKey);
    } catch (e) {
      return null;
    }
  },
  setLayoutPreference: function (pageKey, value) {
    try {
      if (arguments.length === 1 && typeof pageKey === "string") {
        value = pageKey;
        pageKey = null;
      }
      if (!value) return;
      var storageKey = pageKey
        ? "settings-editor-layout:" + pageKey
        : "settings-editor-layout";
      sessionStorage.setItem(storageKey, value);
    } catch (e) {
      /* private mode */
    }
  }
};

window.settingsEditorSplit = (function () {
  var storageKey = "settings-editor-split-list-px";
  var wired = new WeakMap();

  function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }

  function readStoredPx() {
    try {
      var raw = localStorage.getItem(storageKey);
      if (!raw) return null;
      var n = parseFloat(raw);
      return Number.isFinite(n) ? n : null;
    } catch (e) {
      return null;
    }
  }

  function storePx(px) {
    try {
      localStorage.setItem(storageKey, String(Math.round(px)));
    } catch (e) {
      /* private mode */
    }
  }

  function applyListWidth(host, px) {
    host.style.setProperty("--settings-editor-split-list-px", Math.round(px) + "px");
  }

  function wire(host) {
    if (!host || wired.has(host)) return;

    var handle = host.querySelector("[data-split-handle]");
    if (!handle) return;

    var stored = readStoredPx();
    if (stored != null) {
      applyListWidth(host, stored);
    }

    var dragging = false;

    function onPointerDown(e) {
      if (e.button !== 0) return;
      dragging = true;
      handle.setPointerCapture(e.pointerId);
      e.preventDefault();
    }

    function onPointerMove(e) {
      if (!dragging) return;
      var rect = host.getBoundingClientRect();
      var min = 224;
      var max = Math.max(min, rect.width * 0.55);
      var next = clamp(e.clientX - rect.left, min, max);
      applyListWidth(host, next);
    }

    function onPointerUp(e) {
      if (!dragging) return;
      dragging = false;
      try {
        handle.releasePointerCapture(e.pointerId);
      } catch (err) {
        /* ignore */
      }
      var val = host.style.getPropertyValue("--settings-editor-split-list-px");
      if (val) {
        var px = parseFloat(val);
        if (Number.isFinite(px)) storePx(px);
      }
    }

    handle.addEventListener("pointerdown", onPointerDown);
    handle.addEventListener("pointermove", onPointerMove);
    handle.addEventListener("pointerup", onPointerUp);
    handle.addEventListener("pointercancel", onPointerUp);

    wired.set(host, function unwire() {
      handle.removeEventListener("pointerdown", onPointerDown);
      handle.removeEventListener("pointermove", onPointerMove);
      handle.removeEventListener("pointerup", onPointerUp);
      handle.removeEventListener("pointercancel", onPointerUp);
    });
  }

  function unwire(host) {
    if (!host) return;
    var teardown = wired.get(host);
    if (teardown) {
      teardown();
      wired.delete(host);
    }
  }

  function unwireElement(el) {
    if (el) unwire(el);
  }

  return { wire: wire, unwire: unwire, unwireElement: unwireElement };
})();

window.webAppOnboarding = (function () {
  var storageKey = "app-onboarding";

  function read() {
    try {
      var raw = localStorage.getItem(storageKey);
      if (!raw) {
        return { completed: [] };
      }

      var parsed = JSON.parse(raw);
      if (!parsed || !Array.isArray(parsed.completed)) {
        return { completed: [] };
      }

      return { completed: parsed.completed.filter(function (id) {
        return typeof id === "string" && id.length > 0;
      }) };
    } catch (e) {
      return { completed: [] };
    }
  }

  function write(state) {
    try {
      localStorage.setItem(storageKey, JSON.stringify(state));
    } catch (e) {
      /* private mode */
    }
  }

  function indexOf(completed, id) {
    for (var i = 0; i < completed.length; i++) {
      if (completed[i] === id) {
        return i;
      }
    }

    return -1;
  }

  return {
    getCompletedJson: function () {
      return JSON.stringify(read().completed);
    },
    setStepCompleted: function (stepId, completed) {
      if (!stepId) {
        return;
      }

      var state = read();
      var idx = indexOf(state.completed, stepId);
      if (completed) {
        if (idx === -1) {
          state.completed.push(stepId);
        }
      } else if (idx !== -1) {
        state.completed.splice(idx, 1);
      }

      write(state);
    },
    reset: function () {
      write({ completed: [] });
    }
  };
})();
