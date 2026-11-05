// Opens <dialog id="x"> from any [data-dialog-open="x"] and closes the enclosing dialog from any
// [data-dialog-close]. A file, not inline handlers, so it still runs under SecurityHeadersAttribute's CSP.
document.addEventListener("click", function (event) {
  var opener = event.target.closest("[data-dialog-open]");
  if (opener) {
    document.getElementById(opener.getAttribute("data-dialog-open")).showModal();
    return;
  }
  var closer = event.target.closest("[data-dialog-close]");
  if (closer) {
    closer.closest("dialog").close();
  }
});
