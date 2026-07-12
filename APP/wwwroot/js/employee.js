// Employee Self Service - shared client-side behaviour.
// Styling for the sidebar lives in /css/employee.css.
//
// NOTE: the has-children submenu accordion toggle is already handled by
// assets/js/app.js (the same handler the Admin/HR sidebar uses - see the
// "Main navigation" section there, bound on
// '.navigation-main'.find('li').has('ul').children('a')). That handler
// both toggles the 'active' class AND slides the submenu open/closed.
//
// This file used to register a SECOND, competing click handler on the
// same links that only toggled the 'active' class (no slide). Since both
// handlers fired on every click, the two class-toggles cancelled each
// other out, leaving the <li> without its 'active' class even though
// app.js's slideToggle had already run - which is what made the submenu
// look like it wasn't opening. Removed here; app.js's handler alone is
// sufficient and is the one already proven to work on the Admin sidebar.
$(function () {
});
