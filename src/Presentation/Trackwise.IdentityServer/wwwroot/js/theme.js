// Follows the OS light/dark setting with the portals' `.dark-theme` class. Loaded in <head>, before
// first paint, so a dark-mode user never sees a light flash. The portals' stored choice lives in
// their own origin's localStorage, which this host can't read.
(function () {
  var query = window.matchMedia("(prefers-color-scheme: dark)");
  function apply() {
    document.documentElement.classList.toggle("dark-theme", query.matches);
  }
  apply();
  query.addEventListener("change", apply);
})();
