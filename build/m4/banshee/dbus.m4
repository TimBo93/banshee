AC_DEFUN([BANSHEE_CHECK_DBUS_SHARP],
[
	PKG_CHECK_MODULES(DBUS_SHARP_GLIB, dbus-sharp-glib-2.0 >= 0.6)
	AC_SUBST(DBUS_SHARP_GLIB_LIBS)

	PKG_CHECK_MODULES(DBUS_SHARP, dbus-sharp-2.0 >= 0.8)
	AC_SUBST(DBUS_SHARP_LIBS)
])

