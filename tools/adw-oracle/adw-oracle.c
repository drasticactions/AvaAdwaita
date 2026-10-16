/*
 * adw-oracle: builds each scene of the parity corpus with GTK and libadwaita, presents it,
 * and writes what GTK drew: the widget tree with allocations, the render nodes reduced to a
 * normalized list of draws, and a PNG of the scene.
 *
 *   adw-oracle --corpus corpus.json --out expected-gtk.json [--goldens DIR [--scale N]]
 *   adw-oracle --tree TYPE            print the widget tree of one widget type
 *
 * libadwaita reads ADW_DEBUG_HIGH_CONTRAST and ADW_DEBUG_ACCENT_COLOR once, at start-up, so
 * the program runs itself once per (contrast, accent) group and merges the results.
 */

#include <adwaita.h>
#include <glib/gstdio.h>
#include <gtk/gtk.h>
#include <json-glib/json-glib.h>
#include <math.h>
#include <stdio.h>
#include <string.h>

#define MARGIN 12

static GtkWidget *window;
static GtkWidget *host;
static GtkWidget *headerbar;
static GtkCssProvider *scene_css;

/* ---- list models for the tree and column views ------------------------------------------ */

static GListModel *
tree_children (gpointer item, gpointer data)
{
  const char *name = gtk_string_object_get_string (GTK_STRING_OBJECT (item));
  if (g_str_equal (name, "Folder"))
    {
      const char *children[] = { "File one", "File two", NULL };
      return G_LIST_MODEL (gtk_string_list_new (children));
    }
  return NULL;
}

static void
tree_setup (GtkSignalListItemFactory *f, GtkListItem *item, gpointer data)
{
  GtkWidget *expander = gtk_tree_expander_new ();
  GtkWidget *label = gtk_label_new (NULL);
  gtk_label_set_xalign (GTK_LABEL (label), 0);
  gtk_tree_expander_set_child (GTK_TREE_EXPANDER (expander), label);
  gtk_list_item_set_child (item, expander);
}

static void
tree_bind (GtkSignalListItemFactory *f, GtkListItem *item, gpointer data)
{
  GtkTreeListRow *row = gtk_list_item_get_item (item);
  GtkWidget *expander = gtk_list_item_get_child (item);
  gtk_tree_expander_set_list_row (GTK_TREE_EXPANDER (expander), row);
  gtk_label_set_label (GTK_LABEL (gtk_tree_expander_get_child (GTK_TREE_EXPANDER (expander))),
                       gtk_string_object_get_string (gtk_tree_list_row_get_item (row)));
}

static void
cell_setup (GtkSignalListItemFactory *f, GtkListItem *item, gpointer data)
{
  GtkWidget *label = gtk_label_new (NULL);
  gtk_label_set_xalign (GTK_LABEL (label), 0);
  gtk_list_item_set_child (item, label);
}

static void
cell_bind (GtkSignalListItemFactory *f, GtkListItem *item, gpointer data)
{
  const char *name = gtk_string_object_get_string (gtk_list_item_get_item (item));
  char *text = GPOINTER_TO_INT (data) == 0 ? g_strdup (name) : g_strdup_printf ("%d KB", (int) strlen (name) * 3);
  gtk_label_set_label (GTK_LABEL (gtk_list_item_get_child (item)), text);
  g_free (text);
}

/* ---- building widgets ------------------------------------------------------------------ */

static const char *
str_member (JsonObject *o, const char *name, const char *fallback)
{
  return json_object_has_member (o, name) ? json_object_get_string_member (o, name) : fallback;
}

static gboolean
bool_member (JsonObject *o, const char *name)
{
  return json_object_has_member (o, name) && json_object_get_boolean_member (o, name);
}

static double
double_member (JsonObject *o, const char *name, double fallback)
{
  return json_object_has_member (o, name) ? json_object_get_double_member (o, name) : fallback;
}

static GtkWidget *
build (JsonObject *spec)
{
  const char *type = str_member (spec, "type", "button");
  const char *label = str_member (spec, "label", NULL);
  const char *icon = str_member (spec, "icon", NULL);
  double width = double_member (spec, "width", -1);
  GtkWidget *w = NULL;

  if (g_str_equal (type, "button"))
    {
      w = gtk_button_new ();
      if (icon)
        gtk_button_set_icon_name (GTK_BUTTON (w), icon);
      else
        gtk_button_set_label (GTK_BUTTON (w), label ? label : "Button");
    }
  else if (g_str_equal (type, "togglebutton"))
    {
      w = gtk_toggle_button_new ();
      if (icon)
        gtk_button_set_icon_name (GTK_BUTTON (w), icon);
      else
        gtk_button_set_label (GTK_BUTTON (w), label ? label : "Toggle");
      gtk_toggle_button_set_active (GTK_TOGGLE_BUTTON (w), bool_member (spec, "active"));
    }
  else if (g_str_equal (type, "checkbutton") || g_str_equal (type, "radiobutton"))
    {
      w = gtk_check_button_new_with_label (label);
      if (g_str_equal (type, "radiobutton"))
        {
          /* A group leader that is never shown makes the button a radio. */
          GtkWidget *leader = g_object_ref_sink (gtk_check_button_new ());
          gtk_check_button_set_group (GTK_CHECK_BUTTON (w), GTK_CHECK_BUTTON (leader));
        }
      gtk_check_button_set_active (GTK_CHECK_BUTTON (w), bool_member (spec, "active"));
      gtk_check_button_set_inconsistent (GTK_CHECK_BUTTON (w), bool_member (spec, "inconsistent"));
    }
  else if (g_str_equal (type, "switch"))
    {
      w = gtk_switch_new ();
      gtk_switch_set_active (GTK_SWITCH (w), bool_member (spec, "active"));
    }
  else if (g_str_equal (type, "entry"))
    {
      w = gtk_entry_new ();
      if (json_object_has_member (spec, "text"))
        gtk_editable_set_text (GTK_EDITABLE (w), json_object_get_string_member (spec, "text"));
      if (json_object_has_member (spec, "placeholder"))
        gtk_entry_set_placeholder_text (GTK_ENTRY (w), json_object_get_string_member (spec, "placeholder"));
    }
  else if (g_str_equal (type, "label"))
    {
      w = gtk_label_new (label ? label : "Label");
      gtk_label_set_xalign (GTK_LABEL (w), 0);
    }
  else if (g_str_equal (type, "listbox"))
    {
      JsonArray *rows = json_object_get_array_member (spec, "rows");
      w = gtk_list_box_new ();
      gtk_list_box_set_selection_mode (GTK_LIST_BOX (w), GTK_SELECTION_NONE);
      for (guint i = 0; i < json_array_get_length (rows); i++)
        {
          GtkWidget *l = gtk_label_new (json_array_get_string_element (rows, i));
          gtk_label_set_xalign (GTK_LABEL (l), 0);
          gtk_list_box_append (GTK_LIST_BOX (w), l);
        }
    }
  else if (g_str_equal (type, "scale"))
    {
      w = gtk_scale_new_with_range (GTK_ORIENTATION_HORIZONTAL, 0, 100, 1);
      gtk_range_set_value (GTK_RANGE (w), double_member (spec, "value", 40));
      gtk_scale_set_draw_value (GTK_SCALE (w), FALSE);
      if (json_object_has_member (spec, "marks"))
        {
          const char *pos = str_member (spec, "marks", "bottom");
          for (int v = 0; v <= 100; v += 25)
            gtk_scale_add_mark (GTK_SCALE (w), v, g_str_equal (pos, "top") ? GTK_POS_TOP : GTK_POS_BOTTOM, NULL);
        }
    }
  else if (g_str_equal (type, "progressbar"))
    {
      w = gtk_progress_bar_new ();
      gtk_progress_bar_set_fraction (GTK_PROGRESS_BAR (w), double_member (spec, "value", 40) / 100.0);
    }
  else if (g_str_equal (type, "separator"))
    {
      w = gtk_separator_new (GTK_ORIENTATION_HORIZONTAL);
    }
  else if (g_str_equal (type, "menubutton"))
    {
      GMenu *menu = g_menu_new ();
      g_menu_append (menu, "Item", "app.item");
      w = gtk_menu_button_new ();
      gtk_menu_button_set_label (GTK_MENU_BUTTON (w), label ? label : "Menu");
      gtk_menu_button_set_always_show_arrow (GTK_MENU_BUTTON (w), TRUE);
      gtk_menu_button_set_menu_model (GTK_MENU_BUTTON (w), G_MENU_MODEL (menu));
    }
  else if (g_str_equal (type, "splitbutton"))
    {
      GMenu *menu = g_menu_new ();
      g_menu_append (menu, "Item", "app.item");
      w = adw_split_button_new ();
      adw_split_button_set_label (ADW_SPLIT_BUTTON (w), label ? label : "Split");
      adw_split_button_set_menu_model (ADW_SPLIT_BUTTON (w), G_MENU_MODEL (menu));
    }
  else if (g_str_equal (type, "spinbutton"))
    {
      w = gtk_spin_button_new_with_range (0, 100, 1);
      gtk_spin_button_set_value (GTK_SPIN_BUTTON (w), double_member (spec, "value", 40));
    }
  else if (g_str_equal (type, "linkbutton"))
    {
      w = gtk_link_button_new_with_label ("https://example.com", label ? label : "Link");
      gtk_link_button_set_visited (GTK_LINK_BUTTON (w), bool_member (spec, "visited"));
    }
  else if (g_str_equal (type, "notebook"))
    {
      w = gtk_notebook_new ();
      gtk_notebook_append_page (GTK_NOTEBOOK (w), gtk_label_new ("First page"), gtk_label_new ("Tab 1"));
      gtk_notebook_append_page (GTK_NOTEBOOK (w), gtk_label_new ("Second page"), gtk_label_new ("Tab 2"));
      if (json_object_has_member (spec, "position"))
        {
          const char *pos = json_object_get_string_member (spec, "position");
          gtk_notebook_set_tab_pos (GTK_NOTEBOOK (w), g_str_equal (pos, "bottom") ? GTK_POS_BOTTOM : g_str_equal (pos, "left") ? GTK_POS_LEFT : g_str_equal (pos, "right") ? GTK_POS_RIGHT : GTK_POS_TOP);
        }
    }
  else if (g_str_equal (type, "togglegroup"))
    {
      w = adw_toggle_group_new ();
      for (int i = 0; i < 3; i++)
        {
          const char *names[] = { "First", "Second", "Third" };
          AdwToggle *t = adw_toggle_new ();
          adw_toggle_set_label (t, names[i]);
          adw_toggle_group_add (ADW_TOGGLE_GROUP (w), t);
        }
      adw_toggle_group_set_active (ADW_TOGGLE_GROUP (w), 0);
    }
  else if (g_str_equal (type, "expander"))
    {
      w = gtk_expander_new (label ? label : "Expander");
      gtk_expander_set_child (GTK_EXPANDER (w), gtk_label_new ("Content"));
      gtk_expander_set_expanded (GTK_EXPANDER (w), bool_member (spec, "active"));
    }
  else if (g_str_equal (type, "frame"))
    {
      w = gtk_frame_new (label ? label : "Frame");
      gtk_frame_set_child (GTK_FRAME (w), gtk_label_new ("Content"));
    }
  else if (g_str_equal (type, "tooltip"))
    {
      /* GtkTooltipWindow (gtktooltipwindow.ui), which GTK creates only for a pointer hovering: a "tooltip" node
       * with .background holding a box of a hidden image and the label. The stand-in is a box, so it drops the
       * orientation class a box has and the tooltip window does not. */
      GtkWidget *box = gtk_box_new (GTK_ORIENTATION_HORIZONTAL, 0);
      GtkWidget *image = gtk_image_new_from_icon_name ("image-missing");
      GtkWidget *label = gtk_label_new (str_member (spec, "label", "Tip"));
      gtk_widget_set_visible (image, FALSE);
      gtk_widget_set_hexpand (label, TRUE);
      gtk_label_set_wrap (GTK_LABEL (label), TRUE);
      gtk_label_set_max_width_chars (GTK_LABEL (label), 50);
      gtk_box_append (GTK_BOX (box), image);
      gtk_box_append (GTK_BOX (box), label);
      w = g_object_new (GTK_TYPE_BOX, "css-name", "tooltip", NULL);
      gtk_widget_add_css_class (w, "background");
      gtk_widget_remove_css_class (w, "horizontal");
      gtk_box_append (GTK_BOX (w), box);
      gtk_widget_set_halign (w, GTK_ALIGN_START);
    }
  else if (g_str_equal (type, "spinner"))
    {
      w = adw_spinner_new ();
      gtk_widget_set_size_request (w, 32, 32);
    }
  else if (g_str_equal (type, "carouseldots"))
    {
      GtkWidget *carousel = adw_carousel_new ();
      GtkWidget *dots = adw_carousel_indicator_dots_new ();
      gtk_widget_set_size_request (carousel, 200, 40);
      gtk_widget_set_halign (dots, GTK_ALIGN_CENTER);
      for (int i = 0; i < 3; i++)
        adw_carousel_append (ADW_CAROUSEL (carousel), gtk_label_new ("Page"));
      adw_carousel_indicator_dots_set_carousel (ADW_CAROUSEL_INDICATOR_DOTS (dots), ADW_CAROUSEL (carousel));
      w = gtk_box_new (GTK_ORIENTATION_VERTICAL, 0);
      gtk_box_append (GTK_BOX (w), carousel);
      gtk_box_append (GTK_BOX (w), dots);
    }
  else if (g_str_equal (type, "toast"))
    {
      w = adw_toast_overlay_new ();
      adw_toast_overlay_set_child (ADW_TOAST_OVERLAY (w), gtk_label_new (NULL));
      gtk_widget_set_size_request (w, 400, 120);
      adw_toast_overlay_add_toast (ADW_TOAST_OVERLAY (w), adw_toast_new (str_member (spec, "label", "Saved")));
    }
  else if (g_str_equal (type, "paned"))
    {
      w = gtk_paned_new (GTK_ORIENTATION_HORIZONTAL);
      gtk_paned_set_start_child (GTK_PANED (w), gtk_label_new ("Start"));
      gtk_paned_set_end_child (GTK_PANED (w), gtk_label_new ("End"));
      gtk_paned_set_wide_handle (GTK_PANED (w), bool_member (spec, "wide"));
    }
  else if (g_str_equal (type, "splitview"))
    {
      w = adw_overlay_split_view_new ();
      adw_overlay_split_view_set_sidebar (ADW_OVERLAY_SPLIT_VIEW (w), gtk_label_new ("Sidebar"));
      adw_overlay_split_view_set_content (ADW_OVERLAY_SPLIT_VIEW (w), gtk_label_new ("Content"));
    }
  else if (g_str_equal (type, "treeview"))
    {
      const char *roots[] = { "Folder", "Other", NULL };
      GtkTreeListModel *tree = gtk_tree_list_model_new (G_LIST_MODEL (gtk_string_list_new (roots)), FALSE, FALSE, tree_children, NULL, NULL);
      GtkListItemFactory *factory = gtk_signal_list_item_factory_new ();
      g_signal_connect (factory, "setup", G_CALLBACK (tree_setup), NULL);
      g_signal_connect (factory, "bind", G_CALLBACK (tree_bind), NULL);
      w = gtk_list_view_new (GTK_SELECTION_MODEL (gtk_single_selection_new (G_LIST_MODEL (tree))), factory);
      gtk_tree_list_row_set_expanded (gtk_tree_list_model_get_row (tree, 0), bool_member (spec, "active"));
    }
  else if (g_str_equal (type, "columnview") || g_str_equal (type, "datagrid"))
    {
      const char *rows[] = { "Alpha", "Beta", NULL };
      GtkColumnView *view;
      w = gtk_column_view_new (GTK_SELECTION_MODEL (gtk_single_selection_new (G_LIST_MODEL (gtk_string_list_new (rows)))));
      view = GTK_COLUMN_VIEW (w);
      for (int i = 0; i < 2; i++)
        {
          GtkListItemFactory *factory = gtk_signal_list_item_factory_new ();
          GtkColumnViewColumn *column;
          g_signal_connect (factory, "setup", G_CALLBACK (cell_setup), NULL);
          g_signal_connect (factory, "bind", G_CALLBACK (cell_bind), GINT_TO_POINTER (i));
          column = gtk_column_view_column_new (i == 0 ? "Name" : "Size", factory);
          gtk_column_view_column_set_sorter (column, GTK_SORTER (gtk_string_sorter_new (NULL)));
          gtk_column_view_append_column (view, column);
        }
      gtk_column_view_set_show_row_separators (view, bool_member (spec, "row-separators"));
      gtk_column_view_set_show_column_separators (view, bool_member (spec, "column-separators"));
      if (bool_member (spec, "active"))
        gtk_column_view_sort_by_column (view, g_list_model_get_item (gtk_column_view_get_columns (view), 0), GTK_SORT_ASCENDING);
    }
  else if (g_str_equal (type, "spinner"))
    {
      w = adw_spinner_new ();
      gtk_widget_set_size_request (w, 32, 32);
    }
  else if (g_str_equal (type, "carouseldots"))
    {
      GtkWidget *carousel = adw_carousel_new ();
      GtkWidget *dots = adw_carousel_indicator_dots_new ();
      gtk_widget_set_size_request (carousel, 200, 40);
      gtk_widget_set_halign (dots, GTK_ALIGN_CENTER);
      for (int i = 0; i < 3; i++)
        adw_carousel_append (ADW_CAROUSEL (carousel), gtk_label_new ("Page"));
      adw_carousel_indicator_dots_set_carousel (ADW_CAROUSEL_INDICATOR_DOTS (dots), ADW_CAROUSEL (carousel));
      w = gtk_box_new (GTK_ORIENTATION_VERTICAL, 0);
      gtk_box_append (GTK_BOX (w), carousel);
      gtk_box_append (GTK_BOX (w), dots);
    }
  else if (g_str_equal (type, "toast"))
    {
      w = adw_toast_overlay_new ();
      adw_toast_overlay_set_child (ADW_TOAST_OVERLAY (w), gtk_label_new (NULL));
      gtk_widget_set_size_request (w, 400, 120);
      adw_toast_overlay_add_toast (ADW_TOAST_OVERLAY (w), adw_toast_new (str_member (spec, "label", "Saved")));
    }
  else if (g_str_equal (type, "paned"))
    {
      w = gtk_paned_new (GTK_ORIENTATION_HORIZONTAL);
      gtk_paned_set_start_child (GTK_PANED (w), gtk_label_new ("Start"));
      gtk_paned_set_end_child (GTK_PANED (w), gtk_label_new ("End"));
      gtk_paned_set_wide_handle (GTK_PANED (w), bool_member (spec, "active"));
      gtk_widget_set_size_request (w, 200, 60);
      gtk_paned_set_position (GTK_PANED (w), 100);
    }
  else if (g_str_equal (type, "headerbar"))
    w = gtk_label_new (NULL);
  else if (g_str_equal (type, "popovermenu"))
    {
      GMenu *model = g_menu_new ();
      g_menu_append (model, "Open", "app.open");
      g_menu_append (model, "Save As", "app.save");
      g_menu_append (model, "Quit", "app.quit");
      GSimpleActionGroup *actions = g_simple_action_group_new ();
      const char *names[] = { "open", "save", "quit", NULL };
      for (int i = 0; names[i]; i++)
        {
          GSimpleAction *a = g_simple_action_new (names[i], NULL);
          g_action_map_add_action (G_ACTION_MAP (actions), G_ACTION (a));
          g_object_unref (a);
        }
      w = gtk_menu_button_new ();
      gtk_menu_button_set_label (GTK_MENU_BUTTON (w), "Menu");
      gtk_menu_button_set_menu_model (GTK_MENU_BUTTON (w), G_MENU_MODEL (model));
      gtk_widget_set_halign (w, GTK_ALIGN_START);
      /* The items' actions exist, so the items are sensitive. */
      gtk_widget_insert_action_group (w, "app", G_ACTION_GROUP (actions));
      g_object_unref (actions);
      g_object_unref (model);
    }
  else if (g_str_equal (type, "menubar"))
    {
      GMenu *model = g_menu_new ();
      const char *names[] = { "File", "Edit", "View", NULL };
      for (int i = 0; names[i]; i++)
        {
          GMenu *sub = g_menu_new ();
          g_menu_append (sub, "Item", "app.item");
          g_menu_append_submenu (model, names[i], G_MENU_MODEL (sub));
          g_object_unref (sub);
        }
      w = gtk_popover_menu_bar_new_from_model (G_MENU_MODEL (model));
      g_object_unref (model);
    }
  else if (g_str_equal (type, "scrolledwindow"))
    {
      GtkWidget *child = gtk_label_new (NULL);
      gtk_widget_set_size_request (child, 400, 300);
      w = gtk_scrolled_window_new ();
      gtk_scrolled_window_set_overlay_scrolling (GTK_SCROLLED_WINDOW (w), !bool_member (spec, "classic"));
      gtk_scrolled_window_set_child (GTK_SCROLLED_WINDOW (w), child);
      gtk_widget_set_size_request (w, 200, 120);
    }
  else if (g_str_equal (type, "textview"))
    {
      GtkWidget *tv = gtk_text_view_new ();
      gtk_text_buffer_set_text (gtk_text_view_get_buffer (GTK_TEXT_VIEW (tv)), str_member (spec, "text", "Text"), -1);
      w = gtk_scrolled_window_new ();
      gtk_widget_add_css_class (w, "frame");
      gtk_scrolled_window_set_child (GTK_SCROLLED_WINDOW (w), tv);
      gtk_widget_set_size_request (w, 200, 80);
    }
  else if (g_str_equal (type, "calendar"))
    {
      GDateTime *date = g_date_time_new_local (2026, 10, 15, 0, 0, 0);
      w = gtk_calendar_new ();
      gtk_calendar_set_date (GTK_CALENDAR (w), date);
      gtk_calendar_set_show_week_numbers (GTK_CALENDAR (w), bool_member (spec, "active"));
      g_date_time_unref (date);
    }
  else if (g_str_equal (type, "vspinbutton"))
    {
      w = gtk_spin_button_new_with_range (0, 59, 1);
      gtk_orientable_set_orientation (GTK_ORIENTABLE (w), GTK_ORIENTATION_VERTICAL);
      gtk_spin_button_set_value (GTK_SPIN_BUTTON (w), double_member (spec, "value", 30));
    }
  else if (g_str_equal (type, "dropdown"))
    {
      const char *items[] = { "First", "Second", NULL };
      w = gtk_drop_down_new_from_strings (items);
    }
  else
    {
      g_printerr ("unknown widget type %s\n", type);
      w = gtk_label_new (type);
    }

  if (json_object_has_member (spec, "classes"))
    {
      JsonArray *classes = json_object_get_array_member (spec, "classes");
      for (guint i = 0; i < json_array_get_length (classes); i++)
        gtk_widget_add_css_class (w, json_array_get_string_element (classes, i));
    }

  if (width > 0)
    gtk_widget_set_size_request (w, (int) width, -1);
  gtk_widget_set_halign (w, GTK_ALIGN_START);
  gtk_widget_set_valign (w, GTK_ALIGN_START);
  return w;
}

static void
apply_states (GtkWidget *w, JsonObject *spec)
{
  GtkStateFlags flags = 0;
  JsonArray *states;

  if (!json_object_has_member (spec, "states"))
    return;

  states = json_object_get_array_member (spec, "states");
  for (guint i = 0; i < json_array_get_length (states); i++)
    {
      const char *s = json_array_get_string_element (states, i);
      if (g_str_equal (s, "hover"))
        flags |= GTK_STATE_FLAG_PRELIGHT;
      else if (g_str_equal (s, "active"))
        flags |= GTK_STATE_FLAG_ACTIVE;
      else if (g_str_equal (s, "focus-visible"))
        flags |= GTK_STATE_FLAG_FOCUSED | GTK_STATE_FLAG_FOCUS_VISIBLE | GTK_STATE_FLAG_FOCUS_WITHIN;
      else if (g_str_equal (s, "backdrop"))
        flags |= GTK_STATE_FLAG_BACKDROP;
      else if (g_str_equal (s, "selected"))
        flags |= GTK_STATE_FLAG_SELECTED;
      else if (g_str_equal (s, "disabled"))
        gtk_widget_set_sensitive (w, FALSE);
    }
  gtk_widget_set_state_flags (w, flags, FALSE);

  /* A pointer over a drop-down or menu button is over its inner button, which also takes the keyboard
   * focus; GTK does not derive those flags from the parent's. */
  if ((GTK_IS_DROP_DOWN (w) || GTK_IS_MENU_BUTTON (w)) && gtk_widget_get_first_child (w))
    gtk_widget_set_state_flags (gtk_widget_get_first_child (w),
                                flags & (GTK_STATE_FLAG_PRELIGHT | GTK_STATE_FLAG_ACTIVE | GTK_STATE_FLAG_FOCUSED | GTK_STATE_FLAG_FOCUS_VISIBLE),
                                FALSE);
}

/* ---- waiting for a frame ----------------------------------------------------------------- */

static gboolean painted;

static void
on_after_paint (GdkFrameClock *clock, gpointer data)
{
  painted = TRUE;
}

static void
wait_frames (int n)
{
  GdkFrameClock *clock = gtk_widget_get_frame_clock (window);
  gulong id = g_signal_connect (clock, "after-paint", G_CALLBACK (on_after_paint), NULL);
  for (int i = 0; i < n; i++)
    {
      painted = FALSE;
      gtk_widget_queue_draw (window);
      gdk_frame_clock_request_phase (clock, GDK_FRAME_CLOCK_PHASE_PAINT);
      while (!painted)
        g_main_context_iteration (NULL, TRUE);
    }
  g_signal_handler_disconnect (clock, id);
}

/* ---- the widget tree --------------------------------------------------------------------- */

/* Bounds are reported relative to the root's border box; GTK's own origin is the content box. */
static graphene_point_t tree_origin;

static void
tree (GtkWidget *w, GtkWidget *root, int depth, GString *out)
{
  graphene_rect_t b;
  char **classes;

  /* Mapped, not just visible: a stack's other pages are visible but not shown. */
  if (!gtk_widget_get_mapped (w))
    return;

  g_string_append_printf (out, "%*s%s", depth * 2, "", gtk_widget_get_css_name (w));
  classes = gtk_widget_get_css_classes (w);
  for (char **c = classes; *c; c++)
    g_string_append_printf (out, ".%s", *c);
  g_strfreev (classes);
  if (root && gtk_widget_compute_bounds (w, root, &b))
    g_string_append_printf (out, " [%g,%g,%g,%g]", b.origin.x - tree_origin.x, b.origin.y - tree_origin.y, b.size.width, b.size.height);
  g_string_append_c (out, '\n');

  for (GtkWidget *c = gtk_widget_get_first_child (w); c; c = gtk_widget_get_next_sibling (c))
    tree (c, root, depth + 1, out);
}

/* ---- render nodes -> draws --------------------------------------------------------------- */

typedef struct
{
  double dx, dy, opacity;
  const GskRoundedRect *clip;
  double clip_dx, clip_dy;
} Walk;

/* The node getters return a GdkColor's values without converting them, which is right only for sRGB.
 * The color state sits just before the values (GdkColor in gdkcolorprivate.h); convert as GTK does,
 * through linear sRGB, with no gamut mapping. */
static double
srgb_oetf (double v)
{
  return v > 0.0031308 ? 1.055 * pow (v, 1 / 2.4) - 0.055 : 12.92 * v;
}

static GdkRGBA
to_srgb (const GdkRGBA *raw)
{
  GdkColorState *cs = ((GdkColorState * const *) raw)[-1];
  GdkRGBA out = *raw;
  double l, a, b, L, M, S;

  if (cs == NULL || gdk_color_state_equal (cs, gdk_color_state_get_srgb ()))
    return out;

  if (gdk_color_state_equal (cs, gdk_color_state_get_srgb_linear ()))
    {
      out.red = srgb_oetf (raw->red);
      out.green = srgb_oetf (raw->green);
      out.blue = srgb_oetf (raw->blue);
      return out;
    }

  l = raw->red;
  a = raw->green;
  b = raw->blue;
  if (gdk_color_state_equal (cs, gdk_color_state_get_oklch ()))
    {
      double c = raw->green, h = raw->blue * G_PI / 180;
      a = c * cos (h);
      b = c * sin (h);
    }
  else if (!gdk_color_state_equal (cs, gdk_color_state_get_oklab ()))
    {
      g_printerr ("unexpected color state\n");
      return out;
    }

  L = pow (l + 0.3963377774 * a + 0.2158037573 * b, 3);
  M = pow (l - 0.1055613458 * a - 0.0638541728 * b, 3);
  S = pow (l - 0.0894841775 * a - 1.2914855480 * b, 3);
  out.red = srgb_oetf (4.0767416621 * L - 3.3077115913 * M + 0.2309699292 * S);
  out.green = srgb_oetf (-1.2684380046 * L + 2.6097574011 * M - 0.3413193965 * S);
  out.blue = srgb_oetf (-0.0041960863 * L - 0.7034186147 * M + 1.7076147010 * S);
  return out;
}

/* The color filters (CSS filter: grayscale() and the like) GTK applies with color matrix nodes around what it
 * draws, innermost first. */
typedef struct _Filter Filter;
struct _Filter
{
  const graphene_matrix_t *matrix;
  const graphene_vec4_t *offset;
  const Filter *outer;
};

static const Filter *filters;

static GdkRGBA
filtered (const GdkRGBA *in)
{
  GdkRGBA c = *in;
  for (const Filter *f = filters; f; f = f->outer)
    {
      graphene_vec4_t v, out;
      graphene_vec4_init (&v, c.red, c.green, c.blue, c.alpha);
      graphene_matrix_transform_vec4 (f->matrix, &v, &out);
      graphene_vec4_add (&out, f->offset, &out);
      c.red = CLAMP (graphene_vec4_get_x (&out), 0, 1);
      c.green = CLAMP (graphene_vec4_get_y (&out), 0, 1);
      c.blue = CLAMP (graphene_vec4_get_z (&out), 0, 1);
      c.alpha = CLAMP (graphene_vec4_get_w (&out), 0, 1);
    }
  return c;
}

static void
color_string (char out[10], const GdkRGBA *in, double opacity)
{
  GdkRGBA filtered_color = filtered (in);
  const GdkRGBA *c = &filtered_color;
  int r = (int) round (CLAMP (c->red, 0, 1) * 255);
  int g = (int) round (CLAMP (c->green, 0, 1) * 255);
  int b = (int) round (CLAMP (c->blue, 0, 1) * 255);
  int a = (int) round (CLAMP (c->alpha * opacity, 0, 1) * 255);
  g_snprintf (out, 10, "#%02x%02x%02x%02x", r, g, b, a);
}

static void
add_color (JsonBuilder *j, const char *name, const GdkRGBA *raw, double opacity, gboolean from_node)
{
  char s[10];
  GdkRGBA c = from_node ? to_srgb (raw) : *raw;
  color_string (s, &c, opacity);
  json_builder_set_member_name (j, name);
  json_builder_add_string_value (j, s);
}

static double
r2 (double v)
{
  return round (v * 100) / 100;
}

static void
add_rect (JsonBuilder *j, const graphene_rect_t *r, double dx, double dy)
{
  json_builder_set_member_name (j, "r");
  json_builder_begin_array (j);
  json_builder_add_double_value (j, r2 (r->origin.x + dx));
  json_builder_add_double_value (j, r2 (r->origin.y + dy));
  json_builder_add_double_value (j, r2 (r->size.width));
  json_builder_add_double_value (j, r2 (r->size.height));
  json_builder_end_array (j);
}

static void
add_rounded (JsonBuilder *j, const GskRoundedRect *rr, double dx, double dy)
{
  add_rect (j, &rr->bounds, dx, dy);
  json_builder_set_member_name (j, "rad");
  json_builder_begin_array (j);
  for (int i = 0; i < 4; i++)
    json_builder_add_double_value (j, r2 (rr->corner[i].width));
  json_builder_end_array (j);
}

static void
begin_draw (JsonBuilder *j, const char *kind)
{
  json_builder_begin_object (j);
  json_builder_set_member_name (j, "k");
  json_builder_add_string_value (j, kind);
}

static void walk (GskRenderNode *node, Walk w, JsonBuilder *j);

static void
walk_children (GskRenderNode *node, Walk w, JsonBuilder *j)
{
  guint n = gsk_container_node_get_n_children (node);
  for (guint i = 0; i < n; i++)
    walk (gsk_container_node_get_child (node, i), w, j);
}

static void
draw_icon (const graphene_rect_t *bounds, const GdkRGBA *color, gboolean from_node, Walk w, JsonBuilder *j)
{
  begin_draw (j, "icon");
  add_rect (j, bounds, w.dx, w.dy);
  if (color)
    add_color (j, "c", color, w.opacity, from_node);
  json_builder_end_object (j);
}

static void
walk (GskRenderNode *node, Walk w, JsonBuilder *j)
{
  graphene_rect_t bounds;
  gsk_render_node_get_bounds (node, &bounds);

  switch (gsk_render_node_get_node_type (node))
    {
    case GSK_CONTAINER_NODE:
      walk_children (node, w, j);
      break;

    case GSK_DEBUG_NODE:
      walk (gsk_debug_node_get_child (node), w, j);
      break;

    case GSK_TRANSFORM_NODE:
      {
        GskTransform *t = gsk_transform_node_get_transform (node);
        if (gsk_transform_get_category (t) >= GSK_TRANSFORM_CATEGORY_2D_TRANSLATE)
          {
            float x = 0, y = 0;
            gsk_transform_to_translate (t, &x, &y);
            w.dx += x;
            w.dy += y;
            walk (gsk_transform_node_get_child (node), w, j);
          }
        else
          {
            begin_draw (j, "transform");
            add_rect (j, &bounds, w.dx, w.dy);
            json_builder_end_object (j);
          }
      }
      break;

    case GSK_OPACITY_NODE:
      w.opacity *= gsk_opacity_node_get_opacity (node);
      walk (gsk_opacity_node_get_child (node), w, j);
      break;

    case GSK_CLIP_NODE:
      walk (gsk_clip_node_get_child (node), w, j);
      break;

    case GSK_ROUNDED_CLIP_NODE:
      w.clip = gsk_rounded_clip_node_get_clip (node);
      w.clip_dx = w.dx;
      w.clip_dy = w.dy;
      walk (gsk_rounded_clip_node_get_child (node), w, j);
      break;

    case GSK_COLOR_NODE:
      {
        const GdkRGBA *c = gsk_color_node_get_color (node);
        if (c->alpha * w.opacity <= 0)
          break;
        begin_draw (j, "color");
        if (w.clip && w.clip_dx == w.dx && w.clip_dy == w.dy
            && graphene_rect_contains_rect (&bounds, &w.clip->bounds))
          add_rounded (j, w.clip, w.dx, w.dy);
        else
          add_rect (j, &bounds, w.dx, w.dy);
        add_color (j, "c", c, w.opacity, TRUE);
        json_builder_end_object (j);
      }
      break;

    case GSK_BORDER_NODE:
      {
        const float *widths = gsk_border_node_get_widths (node);
        const GdkRGBA *colors = gsk_border_node_get_colors (node);
        begin_draw (j, "border");
        add_rounded (j, gsk_border_node_get_outline (node), w.dx, w.dy);
        json_builder_set_member_name (j, "w");
        json_builder_begin_array (j);
        for (int i = 0; i < 4; i++)
          json_builder_add_double_value (j, r2 (widths[i]));
        json_builder_end_array (j);
        json_builder_set_member_name (j, "c");
        json_builder_begin_array (j);
        for (int i = 0; i < 4; i++)
          {
            char s[10];
            color_string (s, &colors[i], w.opacity);
            json_builder_add_string_value (j, s);
          }
        json_builder_end_array (j);
        json_builder_end_object (j);
      }
      break;

    case GSK_OUTSET_SHADOW_NODE:
    case GSK_INSET_SHADOW_NODE:
      {
        gboolean inset = gsk_render_node_get_node_type (node) == GSK_INSET_SHADOW_NODE;
        const GskRoundedRect *outline = inset ? gsk_inset_shadow_node_get_outline (node) : gsk_outset_shadow_node_get_outline (node);
        const GdkRGBA *c = inset ? gsk_inset_shadow_node_get_color (node) : gsk_outset_shadow_node_get_color (node);
        begin_draw (j, inset ? "inset-shadow" : "shadow");
        add_rounded (j, outline, w.dx, w.dy);
        add_color (j, "c", c, w.opacity, TRUE);
        json_builder_set_member_name (j, "s");
        json_builder_begin_array (j);
        json_builder_add_double_value (j, r2 (inset ? gsk_inset_shadow_node_get_dx (node) : gsk_outset_shadow_node_get_dx (node)));
        json_builder_add_double_value (j, r2 (inset ? gsk_inset_shadow_node_get_dy (node) : gsk_outset_shadow_node_get_dy (node)));
        json_builder_add_double_value (j, r2 (inset ? gsk_inset_shadow_node_get_spread (node) : gsk_outset_shadow_node_get_spread (node)));
        json_builder_add_double_value (j, r2 (inset ? gsk_inset_shadow_node_get_blur_radius (node) : gsk_outset_shadow_node_get_blur_radius (node)));
        json_builder_end_array (j);
        json_builder_end_object (j);
      }
      break;

    case GSK_TEXT_NODE:
      {
        PangoFont *font = gsk_text_node_get_font (node);
        PangoFontDescription *d = pango_font_describe_with_absolute_size (font);
        begin_draw (j, "text");
        add_rect (j, &bounds, w.dx, w.dy);
        add_color (j, "c", gsk_text_node_get_color (node), w.opacity, TRUE);
        json_builder_set_member_name (j, "family");
        json_builder_add_string_value (j, pango_font_description_get_family (d));
        json_builder_set_member_name (j, "weight");
        json_builder_add_int_value (j, pango_font_description_get_weight (d));
        json_builder_set_member_name (j, "size");
        json_builder_add_double_value (j, r2 ((double) pango_font_description_get_size (d) / PANGO_SCALE));
        json_builder_end_object (j);
        pango_font_description_free (d);
      }
      break;

    case GSK_FILL_NODE:
      {
        GskRenderNode *child = gsk_fill_node_get_child (node);
        const GdkRGBA *c = gsk_render_node_get_node_type (child) == GSK_COLOR_NODE ? gsk_color_node_get_color (child) : NULL;
        draw_icon (&bounds, c, TRUE, w, j);
      }
      break;

    case GSK_MASK_NODE:
      {
        GskRenderNode *source = gsk_mask_node_get_source (node);
        const GdkRGBA *c = gsk_render_node_get_node_type (source) == GSK_COLOR_NODE ? gsk_color_node_get_color (source) : NULL;
        draw_icon (&bounds, c, TRUE, w, j);
      }
      break;

    case GSK_COLOR_MATRIX_NODE:
      if (gsk_render_node_get_node_type (gsk_color_matrix_node_get_child (node)) != GSK_TEXTURE_NODE &&
          gsk_render_node_get_node_type (gsk_color_matrix_node_get_child (node)) != GSK_TEXTURE_SCALE_NODE)
        {
          /* A color filter over what the widget draws. */
          Filter f = { gsk_color_matrix_node_get_color_matrix (node), gsk_color_matrix_node_get_color_offset (node), filters };
          filters = &f;
          walk (gsk_color_matrix_node_get_child (node), w, j);
          filters = f.outer;
          break;
        }
      {
        /* Symbolic recoloring: the matrix's offset row is the color. */
        const graphene_vec4_t *off = gsk_color_matrix_node_get_color_offset (node);
        GdkRGBA c = { graphene_vec4_get_x (off), graphene_vec4_get_y (off), graphene_vec4_get_z (off), 1 };
        const graphene_matrix_t *m = gsk_color_matrix_node_get_color_matrix (node);
        c.alpha = graphene_matrix_get_value (m, 3, 3);
        draw_icon (&bounds, &c, FALSE, w, j);
      }
      break;

    case GSK_TEXTURE_NODE:
    case GSK_TEXTURE_SCALE_NODE:
      draw_icon (&bounds, NULL, FALSE, w, j);
      break;

    case GSK_LINEAR_GRADIENT_NODE:
    case GSK_REPEATING_LINEAR_GRADIENT_NODE:
    case GSK_RADIAL_GRADIENT_NODE:
    case GSK_REPEATING_RADIAL_GRADIENT_NODE:
    case GSK_CONIC_GRADIENT_NODE:
      begin_draw (j, "gradient");
      add_rect (j, &bounds, w.dx, w.dy);
      json_builder_end_object (j);
      break;

    case GSK_CAIRO_NODE:
      begin_draw (j, "cairo");
      add_rect (j, &bounds, w.dx, w.dy);
      json_builder_end_object (j);
      break;

    case GSK_CROSS_FADE_NODE:
      walk (gsk_cross_fade_node_get_end_child (node), w, j);
      break;

    case GSK_SHADOW_NODE:
      walk (gsk_shadow_node_get_child (node), w, j);
      break;

    case GSK_BLEND_NODE:
      walk (gsk_blend_node_get_bottom_child (node), w, j);
      walk (gsk_blend_node_get_top_child (node), w, j);
      break;

    case GSK_STROKE_NODE:
      {
        /* A stroked icon shape (GTK's own icons are strokes): its ink is the path widened by half the line. */
        GskRenderNode *child = gsk_stroke_node_get_child (node);
        const GdkRGBA *c = gsk_render_node_get_node_type (child) == GSK_COLOR_NODE ? gsk_color_node_get_color (child) : NULL;
        graphene_rect_t ink;
        float half = gsk_stroke_get_line_width (gsk_stroke_node_get_stroke (node)) / 2;
        if (!gsk_path_get_bounds (gsk_stroke_node_get_path (node), &ink))
          break;
        graphene_rect_inset (&ink, -half, -half);
        draw_icon (&ink, c, TRUE, w, j);
      }
      break;

    default:
      begin_draw (j, "other");
      json_builder_set_member_name (j, "type");
      json_builder_add_int_value (j, gsk_render_node_get_node_type (node));
      add_rect (j, &bounds, w.dx, w.dy);
      json_builder_end_object (j);
      break;
    }
}

static GskRenderNode *
snapshot_widget (GtkWidget *w)
{
  GdkPaintable *p = gtk_widget_paintable_new (w);
  GtkSnapshot *s = gtk_snapshot_new ();
  GskRenderNode *node;
  /* At its intrinsic size the paintable draws the border box at the origin, unscaled. */
  gdk_paintable_snapshot (p, s, gdk_paintable_get_intrinsic_width (p), gdk_paintable_get_intrinsic_height (p));
  node = gtk_snapshot_free_to_node (s);
  g_object_unref (p);
  return node;
}

/* The goldens' device scale. At 2 the oracle runs on a scale-2 output, so GTK picks its scale-2 resources,
 * and the window's nodes are drawn twice as large into the texture. */
static int golden_scale = 1;

static void
save_golden (GtkWidget *w, const char *dir, const char *id)
{
  graphene_rect_t b, area;
  GskRenderNode *node;
  GdkTexture *tex;
  char *path;
  /* A popover is a surface of its own: render it, not the window. */
  GtkWidget *surface = GTK_IS_NATIVE (w) ? w : window;

  if (!gtk_widget_compute_bounds (w, surface, &b))
    return;

  area = GRAPHENE_RECT_INIT (b.origin.x - MARGIN, b.origin.y - MARGIN, b.size.width + 2 * MARGIN, b.size.height + 2 * MARGIN);
  node = snapshot_widget (surface);
  if (golden_scale != 1)
    {
      GtkSnapshot *s = gtk_snapshot_new ();
      gtk_snapshot_scale (s, golden_scale, golden_scale);
      gtk_snapshot_append_node (s, node);
      gsk_render_node_unref (node);
      node = gtk_snapshot_free_to_node (s);
      graphene_rect_scale (&area, golden_scale, golden_scale, &area);
    }
  tex = gsk_renderer_render_texture (gtk_native_get_renderer (GTK_NATIVE (surface)), node, &area);
  path = g_strdup_printf ("%s/%s.png", dir, id);
  gdk_texture_save_to_png (tex, path);
  g_free (path);
  g_object_unref (tex);
  gsk_render_node_unref (node);
}

/* ---- running the corpus ------------------------------------------------------------------ */

static void
setup_window (void)
{
  GtkWidget *tv = adw_toolbar_view_new ();
  GtkSettings *settings = gtk_settings_get_default ();

  /* GNOME's default button layout, as org.gnome.desktop.wm.preferences gives it. */
  g_object_set (settings, "gtk-font-name", "Adwaita Sans 11", "gtk-enable-animations", FALSE, "gtk-cursor-blink", FALSE,
                "gtk-decoration-layout", "appmenu:close", NULL);

  headerbar = adw_header_bar_new ();
  adw_toolbar_view_add_top_bar (ADW_TOOLBAR_VIEW (tv), headerbar);
  host = gtk_box_new (GTK_ORIENTATION_VERTICAL, 0);
  gtk_widget_set_margin_start (host, MARGIN * 2);
  gtk_widget_set_margin_top (host, MARGIN * 2);
  gtk_widget_set_margin_end (host, MARGIN * 2);
  gtk_widget_set_margin_bottom (host, MARGIN * 2);
  adw_toolbar_view_set_content (ADW_TOOLBAR_VIEW (tv), host);
  window = adw_window_new ();
  gtk_window_set_title (GTK_WINDOW (window), "AvaAdwaita");
  adw_window_set_content (ADW_WINDOW (window), tv);
  gtk_window_set_default_size (GTK_WINDOW (window), 640, 480);
  gtk_window_present (GTK_WINDOW (window));
  wait_frames (2);
}

static void
run_entry (JsonObject *entry, JsonBuilder *j, const char *goldens)
{
  const char *id = json_object_get_string_member (entry, "id");
  JsonObject *spec = json_object_get_object_member (entry, "widget");
  gboolean dark = bool_member (entry, "dark");
  AdwStyleManager *sm = adw_style_manager_get_default ();
  GtkWidget *w, *subject;
  GString *t = g_string_new (NULL);
  GskRenderNode *node;
  Walk walk0 = { 0, 0, 1, NULL, 0, 0 };

  adw_style_manager_set_color_scheme (sm, dark ? ADW_COLOR_SCHEME_FORCE_DARK : ADW_COLOR_SCHEME_FORCE_LIGHT);

  /* A scene's own application style sheet. */
  if (json_object_has_member (spec, "css"))
    {
      scene_css = gtk_css_provider_new ();
      gtk_css_provider_load_from_string (scene_css, str_member (spec, "css", ""));
      gtk_style_context_add_provider_for_display (gdk_display_get_default (), GTK_STYLE_PROVIDER (scene_css),
                                                  GTK_STYLE_PROVIDER_PRIORITY_APPLICATION);
    }
  g_object_set (gtk_settings_get_default (), "gtk-show-status-shapes", bool_member (spec, "shapes"), NULL);
  w = build (spec);
  subject = w;
  gtk_box_append (GTK_BOX (host), w);
  if (bool_member (entry, "rtl"))
    gtk_widget_set_direction (w, GTK_TEXT_DIR_RTL);
  /* A header bar scene is the window's own header bar; its states are the window's, as a window goes to the
   * background as a whole. */
  if (g_str_equal (str_member (spec, "type", ""), "headerbar"))
    subject = headerbar;
  apply_states (subject == headerbar ? window : subject, spec);
  wait_frames (2);

  /* No widget keeps keyboard focus (a text view takes it when it maps), so no caret shows; focus states come
   * from apply_states. */
  gtk_window_set_focus (GTK_WINDOW (window), NULL);
  wait_frames (1);

  /* A carousel scene is its indicator dots, measured again once the carousel has its pages' snap points, as in a
   * window that has been laid out before. */
  if (g_str_equal (str_member (spec, "type", ""), "carouseldots"))
    {
      subject = gtk_widget_get_last_child (w);
      gtk_widget_queue_resize (subject);
      wait_frames (2);
    }

  /* A toast scene is the toast the overlay shows. */
  if (g_str_equal (str_member (spec, "type", ""), "toast"))
    {
      for (GtkWidget *c = gtk_widget_get_first_child (w); c; c = gtk_widget_get_next_sibling (c))
        if (g_str_equal (gtk_widget_get_css_name (c), "toast"))
          subject = c;
    }

  /* A menu scene is the popover the button opens. */
  if (g_str_equal (str_member (spec, "type", ""), "popovermenu"))
    {
      gtk_menu_button_popup (GTK_MENU_BUTTON (w));
      wait_frames (4);
      subject = GTK_WIDGET (gtk_menu_button_get_popover (GTK_MENU_BUTTON (w)));
    }

  json_builder_set_member_name (j, id);
  json_builder_begin_object (j);
  {
    graphene_rect_t b;
    if (!gtk_widget_compute_bounds (subject, subject, &b))
      graphene_rect_init (&b, 0, 0, 0, 0);
    tree_origin = b.origin;
    json_builder_set_member_name (j, "size");
    json_builder_begin_array (j);
    json_builder_add_double_value (j, b.size.width);
    json_builder_add_double_value (j, b.size.height);
    json_builder_end_array (j);
  }
  tree (subject, subject, 0, t);
  json_builder_set_member_name (j, "tree");
  json_builder_add_string_value (j, t->str);
  json_builder_set_member_name (j, "draws");
  json_builder_begin_array (j);
  node = snapshot_widget (subject);
  if (node)
    {
      walk (node, walk0, j);
      gsk_render_node_unref (node);
    }
  json_builder_end_array (j);
  json_builder_end_object (j);

  if (goldens)
    save_golden (subject, goldens, id);

  if (scene_css)
    {
      gtk_style_context_remove_provider_for_display (gdk_display_get_default (), GTK_STYLE_PROVIDER (scene_css));
      g_clear_object (&scene_css);
    }
  if (GTK_IS_POPOVER (subject))
    gtk_popover_popdown (GTK_POPOVER (subject));
  if (subject == headerbar)
    gtk_widget_unset_state_flags (window, GTK_STATE_FLAG_BACKDROP | GTK_STATE_FLAG_PRELIGHT | GTK_STATE_FLAG_ACTIVE);
  gtk_box_remove (GTK_BOX (host), w);
  g_string_free (t, TRUE);
}

static const char *
entry_group (JsonObject *entry)
{
  static char buf[64];
  g_snprintf (buf, sizeof buf, "%s:%s", bool_member (entry, "hc") ? "1" : "0", str_member (entry, "accent", "blue"));
  return buf;
}

/* Runs the entries of one group in this process. */
static int
run_group (JsonArray *entries, const char *group, const char *out, const char *goldens)
{
  JsonBuilder *j = json_builder_new ();
  JsonGenerator *gen = json_generator_new ();
  JsonNode *root;
  GError *error = NULL;

  setup_window ();
  json_builder_begin_object (j);
  for (guint i = 0; i < json_array_get_length (entries); i++)
    {
      JsonObject *e = json_array_get_object_element (entries, i);
      if (g_str_equal (entry_group (e), group))
        run_entry (e, j, goldens);
    }
  json_builder_end_object (j);

  root = json_builder_get_root (j);
  json_generator_set_root (gen, root);
  if (!json_generator_to_file (gen, out, &error))
    {
      g_printerr ("%s\n", error->message);
      return 1;
    }
  json_node_unref (root);
  g_object_unref (gen);
  g_object_unref (j);
  return 0;
}

/* Runs each group in a child process with libadwaita's debug variables set, then merges. */
static int
run_all (const char *self, const char *corpus, JsonArray *entries, const char *out, const char *goldens)
{
  GPtrArray *groups = g_ptr_array_new_with_free_func (g_free);
  JsonObject *merged = json_object_new ();
  JsonNode *root = json_node_new (JSON_NODE_OBJECT);
  JsonGenerator *gen = json_generator_new ();
  GError *error = NULL;

  for (guint i = 0; i < json_array_get_length (entries); i++)
    {
      const char *g = entry_group (json_array_get_object_element (entries, i));
      gboolean seen = FALSE;
      for (guint k = 0; k < groups->len; k++)
        seen |= g_str_equal (g_ptr_array_index (groups, k), g);
      if (!seen)
        g_ptr_array_add (groups, g_strdup (g));
    }

  for (guint k = 0; k < groups->len; k++)
    {
      const char *g = g_ptr_array_index (groups, k);
      char **parts = g_strsplit (g, ":", 2);
      char *part_out = g_strdup_printf ("%s.%u", out, k);
      char **env = g_get_environ ();
      int status = 0;
      JsonParser *parser = json_parser_new ();
      char scale[16];
      const char *argv[] = { self, "--corpus", corpus, "--out", part_out, "--group", g, "--scale", scale, goldens ? "--goldens" : NULL, goldens, NULL };

      g_snprintf (scale, sizeof scale, "%d", golden_scale);

      env = g_environ_setenv (env, "ADW_DEBUG_HIGH_CONTRAST", parts[0], TRUE);
      env = g_environ_setenv (env, "ADW_DEBUG_ACCENT_COLOR", parts[1], TRUE);
      g_printerr ("group hc=%s accent=%s\n", parts[0], parts[1]);
      if (!g_spawn_sync (NULL, (char **) argv, env, G_SPAWN_CHILD_INHERITS_STDIN, NULL, NULL, NULL, NULL, &status, &error)
          || !g_spawn_check_wait_status (status, &error))
        {
          g_printerr ("%s\n", error->message);
          return 1;
        }

      if (!json_parser_load_from_file (parser, part_out, &error))
        {
          g_printerr ("%s\n", error->message);
          return 1;
        }

      JsonObject *part = json_node_get_object (json_parser_get_root (parser));
      GList *members = json_object_get_members (part);
      for (GList *m = members; m; m = m->next)
        json_object_set_member (merged, m->data, json_node_copy (json_object_get_member (part, m->data)));
      g_list_free (members);
      g_unlink (part_out);
      g_object_unref (parser);
      g_free (part_out);
      g_strfreev (parts);
      g_strfreev (env);
    }

  /* Keep the corpus order so the file diffs well. */
  {
    JsonObject *ordered = json_object_new ();
    for (guint i = 0; i < json_array_get_length (entries); i++)
      {
        const char *id = json_object_get_string_member (json_array_get_object_element (entries, i), "id");
        if (json_object_has_member (merged, id))
          json_object_set_member (ordered, id, json_node_copy (json_object_get_member (merged, id)));
      }
    json_node_take_object (root, ordered);
  }

  json_generator_set_root (gen, root);
  json_generator_set_pretty (gen, TRUE);
  json_generator_set_indent (gen, 1);
  if (!json_generator_to_file (gen, out, &error))
    {
      g_printerr ("%s\n", error->message);
      return 1;
    }
  json_object_unref (merged);
  return 0;
}

int
main (int argc, char **argv)
{
  const char *corpus = NULL, *out = NULL, *goldens = NULL, *group = NULL, *tree_type = NULL;
  JsonParser *parser;
  GError *error = NULL;

  for (int i = 1; i < argc; i++)
    {
      if (g_str_equal (argv[i], "--corpus") && i + 1 < argc)
        corpus = argv[++i];
      else if (g_str_equal (argv[i], "--out") && i + 1 < argc)
        out = argv[++i];
      else if (g_str_equal (argv[i], "--goldens") && i + 1 < argc)
        goldens = argv[++i];
      else if (g_str_equal (argv[i], "--group") && i + 1 < argc)
        group = argv[++i];
      else if (g_str_equal (argv[i], "--tree") && i + 1 < argc)
        tree_type = argv[++i];
      else if (g_str_equal (argv[i], "--scale") && i + 1 < argc)
        {
          int n = atoi (argv[++i]);
          golden_scale = n > 1 ? n : 1;
        }
      else
        {
          g_printerr ("usage: adw-oracle --corpus FILE --out FILE [--goldens DIR [--scale N]] | --tree TYPE\n");
          return 2;
        }
    }

  adw_init ();

  if (tree_type)
    {
      JsonObject *spec = json_object_new ();
      GString *t = g_string_new (NULL);
      GtkWidget *w;
      json_object_set_string_member (spec, "type", tree_type);
      if (g_str_equal (tree_type, "listbox"))
        {
          JsonArray *rows = json_array_new ();
          json_array_add_string_element (rows, "Row");
          json_object_set_array_member (spec, "rows", rows);
        }
      setup_window ();
      w = build (spec);
      gtk_box_append (GTK_BOX (host), w);
      wait_frames (2);
      {
        graphene_rect_t b;
        if (!gtk_widget_compute_bounds (w, w, &b))
          graphene_rect_init (&b, 0, 0, 0, 0);
        tree_origin = b.origin;
      }
      tree (w, w, 0, t);
      fputs (t->str, stdout);
      return 0;
    }

  if (!corpus || !out)
    {
      g_printerr ("usage: adw-oracle --corpus FILE --out FILE [--goldens DIR [--scale N]] | --tree TYPE\n");
      return 2;
    }

  parser = json_parser_new ();
  if (!json_parser_load_from_file (parser, corpus, &error))
    {
      g_printerr ("%s\n", error->message);
      return 1;
    }

  {
    JsonArray *entries = json_node_get_array (json_parser_get_root (parser));
    if (group)
      return run_group (entries, group, out, goldens);
    return run_all (argv[0], corpus, entries, out, goldens);
  }
}
