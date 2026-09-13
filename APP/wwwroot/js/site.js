// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ============================================================================
// SweetAlert delete confirmation - project-wide standard.
//
// Every module's Index/Details view used to do this inline, per delete
// link/button:
//   <a asp-action="Delete" asp-route-id="@item.Id"
//      onclick="return confirm('Are you sure you want to delete this department?');">
//
// Native browser confirm() is SYNCHRONOUS - the onclick handler blocks and
// returns true/false immediately - which is exactly why it can't just be
// swapped for "return swal(...)" in place: SweetAlert is asynchronous (it
// shows a popup and calls a callback later). confirmSweetDelete() below
// bridges that gap: it always returns false immediately (cancelling the
// link's default navigation), then does the actual navigation itself, from
// inside SweetAlert's confirm callback, only if the user clicked Yes.
//
// Standard replacement across every view in the project - same three
// arguments every time, nothing else changes on the element itself:
//   <a asp-action="Delete" asp-route-id="@item.Id"
//      onclick="return confirmSweetDelete(event, this, 'Are you sure you want to delete this department?');">
//
// For a delete that must be a POST (anti-forgery-protected form, not a GET
// link), use confirmSweetDeleteForm on the form's onsubmit instead:
//   <form asp-action="Delete" asp-route-id="@item.Id" method="post"
//         onsubmit="return confirmSweetDeleteForm(event, this, 'Are you sure you want to delete this department?');">
//       @Html.AntiForgeryToken()
//       <button type="submit">...</button>
//   </form>
// ============================================================================

function confirmSweetDelete(event, linkEl, message) {
    if (event && event.preventDefault) event.preventDefault();

    var href = linkEl.getAttribute("href") || linkEl.href;
    var confirmText = "Are you sure?";
    var text = message || "You will not be able to recover this record!";

    if (typeof swal !== "function") {
        // sweet_alert.min.js not loaded on this page for some reason - fail
        // safe to the native confirm rather than silently deleting with no
        // confirmation at all.
        if (window.confirm(text)) {
            window.location.href = href;
        }
        return false;
    }

    swal({
        title: confirmText,
        text: text,
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "Cancel",
        closeOnConfirm: true
    }, function (isConfirm) {
        if (isConfirm) {
            window.location.href = href;
        }
    });

    return false;
}

function confirmSweetDeleteForm(event, formEl, message) {
    if (formEl.dataset.swalConfirmed === "true") {
        // Already confirmed below - let the real submit through this time.
        return true;
    }

    if (event && event.preventDefault) event.preventDefault();

    var text = message || "You will not be able to recover this record!";

    if (typeof swal !== "function") {
        if (window.confirm(text)) {
            formEl.dataset.swalConfirmed = "true";
            formEl.submit();
        }
        return false;
    }

    swal({
        title: "Are you sure?",
        text: text,
        type: "warning",
        showCancelButton: true,
        confirmButtonColor: "#DD6B55",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "Cancel",
        closeOnConfirm: true
    }, function (isConfirm) {
        if (isConfirm) {
            formEl.dataset.swalConfirmed = "true";
            formEl.submit();
        }
    });

    return false;
}
