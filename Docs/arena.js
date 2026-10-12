// Shared helpers for the Arena-Baukasten pages: navigation, saving on this Mac, toast messages, sounds.
(function () {
  "use strict";

  const PAGES = [
    { id: "start", href: "index.html", label: "Start" },
    { id: "spickzettel", href: "Spickzettel.html", label: "Spickzettel" },
    { id: "challenges", href: "Challenges.html", label: "Challenges" },
    { id: "remix", href: "Remix.html", label: "Remix-Würfel" },
    { id: "spielidee", href: "Spielidee.html", label: "Spielidee" },
    { id: "feedback", href: "Feedback.html", label: "Feedback-Box" },
  ];

  // Saved in the browser of this Mac. Can be unavailable (private mode, blocked storage): then nothing is saved.
  const store = {
    get(key, fallback) {
      try {
        const raw = localStorage.getItem("arenakit." + key);
        return raw === null ? fallback : JSON.parse(raw);
      } catch (e) {
        return fallback;
      }
    },
    set(key, value) {
      try {
        localStorage.setItem("arenakit." + key, JSON.stringify(value));
        return true;
      } catch (e) {
        return false;
      }
    },
    remove(key) {
      try {
        localStorage.removeItem("arenakit." + key);
      } catch (e) { /* nothing to do */ }
    },
  };

  function toast(message) {
    let element = document.querySelector(".toast");
    if (!element) {
      element = document.createElement("div");
      element.className = "toast";
      element.setAttribute("role", "status");
      document.body.appendChild(element);
    }
    element.textContent = message;
    element.classList.add("show");
    clearTimeout(toast.timer);
    toast.timer = setTimeout(() => element.classList.remove("show"), 2200);
  }

  let audio = null;
  /** A short "ding" (or a few), made with Web Audio so no sound files are needed. */
  function chime(times) {
    try {
      audio = audio || new (window.AudioContext || window.webkitAudioContext)();
      const start = audio.currentTime;
      for (let i = 0; i < (times || 1); i++) {
        [880, 1320].forEach((frequency, n) => {
          const oscillator = audio.createOscillator();
          const gain = audio.createGain();
          oscillator.type = "sine";
          oscillator.frequency.value = frequency;
          const at = start + i * 0.45 + n * 0.12;
          gain.gain.setValueAtTime(0.0001, at);
          gain.gain.exponentialRampToValueAtTime(0.35, at + 0.02);
          gain.gain.exponentialRampToValueAtTime(0.0001, at + 0.4);
          oscillator.connect(gain).connect(audio.destination);
          oscillator.start(at);
          oscillator.stop(at + 0.45);
        });
      }
    } catch (e) { /* no sound available */ }
  }

  function buildTopbar() {
    const bar = document.querySelector("nav.topbar");
    if (!bar) return;
    const current = bar.dataset.page;
    const brand = document.createElement("a");
    brand.className = "brand";
    brand.href = "index.html";
    brand.textContent = "Arena-Baukasten";
    bar.appendChild(brand);
    PAGES.forEach((page) => {
      const link = document.createElement("a");
      link.className = "nav";
      link.href = page.href;
      link.textContent = page.label;
      if (page.id === current) link.setAttribute("aria-current", "page");
      bar.appendChild(link);
    });
  }

  function addCopyButtons() {
    document.querySelectorAll("pre.code").forEach((pre) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "btn small copy no-print";
      button.textContent = "Kopieren";
      button.addEventListener("click", () => copyText(pre.innerText));
      pre.insertAdjacentElement("beforebegin", button);
    });
  }

  function copyText(text) {
    const done = () => toast("Code kopiert!");
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(done, () => fallbackCopy(text, done));
    } else {
      fallbackCopy(text, done);
    }
  }

  function fallbackCopy(text, done) {
    const area = document.createElement("textarea");
    area.value = text;
    document.body.appendChild(area);
    area.select();
    try {
      document.execCommand("copy");
      done();
    } catch (e) {
      toast("Kopieren ging nicht. Bitte markieren und ⌘+C drücken.");
    }
    area.remove();
  }

  function escapeHtml(text) {
    return String(text).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  }

  window.Arena = { store, toast, chime, escapeHtml, copyText };

  document.addEventListener("DOMContentLoaded", () => {
    buildTopbar();
    addCopyButtons();
  });
})();
