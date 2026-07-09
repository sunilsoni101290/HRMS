// Employee Self Service - shared client-side behaviour.
// Currently just ensures accordion-style submenus in the sidebar collapse
// correctly; styling itself lives in /css/employee.css.
$(function () {
    $(".navigation-main > li.has-children > a").on("click", function (e) {
        e.preventDefault();
        $(this).closest("li").toggleClass("active");
    });
});
