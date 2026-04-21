window.webAppShell = (function () {
  var mq = window.matchMedia ? window.matchMedia("(max-width: 640px)") : null;

  function setSidebarClass(shellId, collapsed) {
    var el = document.getElementById(shellId);
    if (!el) return;
    if (collapsed) el.classList.add("sidebar-collapsed");
    else el.classList.remove("sidebar-collapsed");
  }

  function isCollapsed(shell) {
    return shell.classList.contains("sidebar-collapsed");
  }

  function syncShellUi(shellId) {
    var shell = document.getElementById(shellId);
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

  function setSidebarCollapsed(shellId, collapsed) {
    setSidebarClass(shellId, collapsed);
    syncShellUi(shellId);
  }

  function collapseSidebar(shellId) {
    setSidebarCollapsed(shellId, true);
  }

  function collapseSidebarIfMobile(shellId) {
    if (mq && mq.matches) {
      collapseSidebar(shellId);
    }
  }

  function applyViewportDefault(shellId) {
    var shell = document.getElementById(shellId);
    if (!shell) return;
    if (mq && mq.matches) {
      setSidebarClass(shellId, true);
    } else {
      setSidebarClass(shellId, false);
    }
    syncShellUi(shellId);
  }

  function initShell(shellId) {
    var shell = document.getElementById(shellId);
    if (!shell || shell.dataset.shellInit === "1") return;
    shell.dataset.shellInit = "1";

    function close() {
      collapseSidebar(shellId);
    }

    // Delegation: Navbar/Sidebar often render after #app-shell exists; per-button
    // listeners would miss them. Capture phase runs before Blazor's delegation.
    shell.addEventListener(
      "click",
      function (e) {
        var t = e.target;
        var el = t && t.nodeType === 1 ? t : t && t.parentElement;
        if (!el || typeof el.closest !== "function") return;
        var closeEl = el.closest("[data-shell-close]");
        if (closeEl && shell.contains(closeEl)) {
          e.preventDefault();
          close();
          return;
        }
        var toggleEl = el.closest("[data-shell-toggle]");
        if (toggleEl && shell.contains(toggleEl)) {
          e.preventDefault();
          setSidebarClass(shellId, !isCollapsed(shell));
          syncShellUi(shellId);
        }
      },
      true
    );

    document.addEventListener("keydown", function (e) {
      if (e.key !== "Escape") return;
      if (!mq || !mq.matches) return;
      if (isCollapsed(shell)) return;
      close();
    });

    if (mq) {
      var onMedia = function () {
        applyViewportDefault(shellId);
      };
      if (typeof mq.addEventListener === "function") {
        mq.addEventListener("change", onMedia);
      } else if (typeof mq.addListener === "function") {
        mq.addListener(onMedia);
      }
    }

    applyViewportDefault(shellId);
  }

  return {
    setSidebarCollapsed: setSidebarCollapsed,
    syncShellUi: syncShellUi,
    initShell: initShell,
    collapseSidebar: collapseSidebar,
    collapseSidebarIfMobile: collapseSidebarIfMobile
  };
})();

(function () {
  var obs;
  function tryInit() {
    var shell = document.getElementById("app-shell");
    if (!shell) return;
    window.webAppShell.initShell("app-shell");
    if (shell.dataset.shellInit === "1" && obs) {
      obs.disconnect();
    }
  }
  obs = new MutationObserver(tryInit);
  obs.observe(document.documentElement, { childList: true, subtree: true });
  tryInit();
})();

window.webAppScroll = {
  scrollElementToEnd: function (el) {
    if (el) el.scrollTop = el.scrollHeight;
  }
};

window.webAppChat = {
  wireEnter: function (textarea, dotnetHelper) {
    if (!textarea || !dotnetHelper) return;
    textarea.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        dotnetHelper.invokeMethodAsync("SendFromJsAsync");
      }
    });
  }
};
