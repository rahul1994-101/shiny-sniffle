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
