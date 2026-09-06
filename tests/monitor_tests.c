#include <stdio.h>
#include "../Scmpoo/Scmpoo.c"

#define CHECK(condition) do { if (!(condition)) { fprintf(stderr, "FAIL: %s:%d: %s\n", __FILE__, __LINE__, #condition); return 1; } } while (0)

static void set_monitor(UINT index, int left, int top, int right, int bottom)
{
    SetRect(&scmpoo_monitor_work_areas[index], left, top, right, bottom);
}

int main(void)
{
    RECT area;
    RECT window_rect;
    HWND landing_window;
    HWND test_window;
    WNDCLASSW window_class;
    int index;
    int x;

    scmpoo_monitor_count = 2U;
    set_monitor(0, -1920, -200, 0, 880);
    set_monitor(1, 0, 0, 1920, 1040);
    CHECK(scmpoo_get_monitor_rect(-50, -100, &area));
    CHECK(area.left == -1920 && area.top == -200);
    scmpoo_get_walk_bounds(-30, 200, &area);
    CHECK(area.left == -1920 && area.right == 1920);

    /* The same adjacent screens are disconnected above the lower display. */
    scmpoo_get_walk_bounds(-30, -100, &area);
    CHECK(area.right == 0);
    CHECK(!scmpoo_sprite_on_monitor(10, -100));

    set_monitor(1, 500, 0, 2420, 1040);
    scmpoo_get_walk_bounds(-30, 200, &area);
    CHECK(area.right == 0);
    CHECK(!scmpoo_sprite_on_monitor(200, 200));
    CHECK(scmpoo_get_monitor_rect(450, 200, &area) && area.left == 500);

    /* Crossing y = 0 or y = -1 must be a real floor hit, not a sentinel. */
    scmpoo_monitor_count = 1U;
    set_monitor(0, -1000, -600, 0, 0);
    word_CA4C = -1000;
    word_CA4E = -600;
    word_CA50 = 1000;
    word_CA52 = 600;
    word_CA74 = 0;
    scmpoo_window_cache_valid = TRUE;
    scmpoo_window_cache_tick = GetTickCount();
    landing_window = (HWND)1;
    CHECK(sub_408C(&landing_window, 5, -5, -200, -160) == 0);
    CHECK(landing_window == NULL);
    CHECK(sub_408C(&landing_window, -10, -20, -200, -160) == SCMPOO_NO_COLLISION);
    set_monitor(0, -1000, -600, 0, -1);
    CHECK(sub_408C(&landing_window, 5, -5, -200, -160) == -1);
    CHECK(sub_419E(NULL, 5, -5, -200, -160) == SCMPOO_BELOW_FLOOR);

    /* A window ending at x = 0 used to cause division by zero. */
    SetRect(&window_rect, -900, -300, 0, 500);
    for (index = 0; index < 1000; index += 1) {
        x = scmpoo_random_window_x(&window_rect);
        CHECK(x >= -620 && x < -320);
    }
    SetRect(&window_rect, 0, 0, 1, 1);
    CHECK(scmpoo_random_window_x(&window_rect) == -20);

    /* A fresh cache avoids another desktop enumeration even on a fast tick. */
    word_CA74 = 1;
    stru_C0BC[0].window = (HWND)1;
    scmpoo_window_cache_tick = GetTickCount();
    sub_3DF0();
    CHECK(word_CA74 == 1 && stru_C0BC[0].window == (HWND)1);

    /* Simulate disconnecting the screen holding a sleeping sheep. */
    ZeroMemory(&window_class, sizeof(window_class));
    window_class.lpfnWndProc = DefWindowProcW;
    window_class.cbWndExtra = sizeof(LONG_PTR) * 2;
    window_class.hInstance = GetModuleHandleW(NULL);
    window_class.lpszClassName = L"ScmpooMonitorRegression";
    CHECK(RegisterClassW(&window_class) != 0);
    test_window = CreateWindowW(window_class.lpszClassName, L"", WS_POPUP, 0, 0, 40, 40, NULL, NULL, window_class.hInstance, NULL);
    CHECK(test_window != NULL);
    word_C0B0 = test_window;
    set_monitor(0, 0, 0, 1280, 680);
    word_A800 = -1800;
    word_A802 = 900;
    word_A8A0 = 114;
    word_CA42 = 1;
    scmpoo_recover_monitor_position();
    CHECK(word_A800 == 0 && word_A802 == 640);
    CHECK(word_A8A0 == 97 && word_A81C == NULL);
    CHECK(scmpoo_sprite_on_monitor(word_A800, word_A802));
    word_C0B0 = NULL;
    DestroyWindow(test_window);
    UnregisterClassW(window_class.lpszClassName, window_class.hInstance);
    puts("monitor regression checks passed");
    return 0;
}
