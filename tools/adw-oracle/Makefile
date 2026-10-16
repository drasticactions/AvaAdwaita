# Builds adw-oracle against the installed GTK 4 and libadwaita.
OUT ?= .
PKGS = gtk4 libadwaita-1 json-glib-1.0
CFLAGS ?= -O2 -g -Wall -Wno-deprecated-declarations

$(OUT)/adw-oracle: adw-oracle.c
	$(CC) $(CFLAGS) -o $@ $< $$(pkg-config --cflags --libs $(PKGS)) -lm

.PHONY: clean
clean:
	rm -f $(OUT)/adw-oracle
