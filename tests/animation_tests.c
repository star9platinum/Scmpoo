#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <MMSystem.h>
#include <stdio.h>

static DWORD test_tick;
static DWORD WINAPI test_get_tick_count(void);
static BOOL WINAPI test_play_sound_a(LPCSTR, HMODULE, DWORD);
static BOOL WINAPI test_play_sound_w(LPCWSTR, HMODULE, DWORD);

#define GetTickCount test_get_tick_count
#define PlaySoundA test_play_sound_a
#define PlaySoundW test_play_sound_w
#define WinMain scmpoo_application_entry
#include "../Scmpoo/Scmpoo.c"
#undef WinMain
#undef GetTickCount
#undef PlaySoundA
#undef PlaySoundW

static HANDLE sound_entered;
static HANDLE release_sound;
static HANDLE sound_stopped;
static volatile LONG sound_calls;
static volatile LONG sound_resource;
static volatile LONG sound_thread;
static int failures;

#define CHECK(condition) do { \
    if (!(condition)) { \
        fprintf(stderr, "%s:%d: %s\n", __FILE__, __LINE__, #condition); \
        failures += 1; \
    } \
} while (0)

static DWORD WINAPI test_get_tick_count(void)
{
    return test_tick;
}

static BOOL WINAPI test_play_sound_a(LPCSTR sound, HMODULE module, DWORD flags)
{
    LONG call;
    (void)module;
    (void)flags;
    if (sound == NULL) {
        SetEvent(sound_stopped);
        return TRUE;
    }
    InterlockedExchange(&sound_resource, (LONG)(ULONG_PTR)sound);
    InterlockedExchange(&sound_thread, (LONG)GetCurrentThreadId());
    call = InterlockedIncrement(&sound_calls);
    SetEvent(sound_entered);
    if (call == 1) {
        WaitForSingleObject(release_sound, 5000U);
    }
    return TRUE;
}

static BOOL WINAPI test_play_sound_w(LPCWSTR sound, HMODULE module, DWORD flags)
{
    return test_play_sound_a((LPCSTR)sound, module, flags);
}

static void test_chime_cancellation(void)
{
    scmpoo_sound_closing = TRUE;
    word_C0AC = 0;
    word_CA76 = 0;
    word_A832 = 7;
    word_A8A0 = 82;
    sub_46F7();
    CHECK(word_A832 == 0);
    CHECK(word_A8A0 == 1);

    word_CA76 = 1;
    word_A832 = 4;
    word_A8A0 = 82;
    sub_46F7();
    CHECK(word_A832 == 0);
    CHECK(word_A8A0 == 113);

    word_CA76 = 0;
    word_A832 = 4;
    word_A8A0 = 45;
    sub_46F7();
    CHECK(word_A832 == 0);
    CHECK(word_A8A0 == 45);
}

static void test_chime_tick_wrap(void)
{
    word_C0AC = 1;
    word_A832 = 2;
    word_A8A0 = 82;
    dword_A834 = 0xfffffff0U;
    test_tick = 983U;
    sub_46F7();
    CHECK(word_A832 == 2);
    test_tick = 984U;
    sub_46F7();
    CHECK(word_A832 == 1);
    test_tick = 1984U;
    sub_46F7();
    CHECK(word_A832 == 0);
    CHECK(word_A8A0 == 1);
    word_C0AC = 0;
}

static void test_zero_coordinate_peer(void)
{
    WNDCLASSA window_class;
    HWND peer;
    HWND decoy;
    ZeroMemory(&window_class, sizeof(window_class));
    window_class.lpfnWndProc = DefWindowProcA;
    window_class.cbWndExtra = (int)(sizeof(LONG_PTR) * 2);
    window_class.hInstance = GetModuleHandleA(NULL);
    window_class.lpszClassName = "ScreenMatePoo";
    CHECK(RegisterClassA(&window_class) != 0);
    peer = CreateWindowA("ScreenMatePoo", "Screen Mate", 0,
        0, 0, 40, 40, NULL, NULL, window_class.hInstance, NULL);
    CHECK(peer != NULL);
    CHECK(scmpoo_is_sheep_window(peer));
    decoy = CreateWindowA("STATIC", "Screen Mate", 0,
        0, 0, 40, 40, NULL, NULL, window_class.hInstance, NULL);
    CHECK(!scmpoo_is_sheep_window(decoy));
    SetWindowLongPtr(peer, 0, -40);
    SetWindowLongPtr(peer, (int)sizeof(LONG_PTR), 0);
    word_CA60[0] = peer;
    CHECK(sub_3A36(-2, 2, -10, 10) == 0);
    CHECK(sub_3A36(100, 120, -10, 10) == SCMPOO_NO_COLLISION);
    word_CA60[0] = NULL;
    DestroyWindow(decoy);
    DestroyWindow(peer);
}

static void test_animation_during_blocked_audio(void)
{
    DWORD began;
    int index;
    scmpoo_sound_closing = FALSE;
    sound_entered = CreateEventA(NULL, FALSE, FALSE, NULL);
    release_sound = CreateEventA(NULL, TRUE, FALSE, NULL);
    sound_stopped = CreateEventA(NULL, FALSE, FALSE, NULL);
    sub_4210(108, 0U, 0);
    CHECK(WaitForSingleObject(sound_entered, 2000U) == WAIT_OBJECT_0);
    began = GetTickCount();
    for (index = 0; index < 10000; index += 1) {
        sub_4210(109, 0U, 0);
    }
    word_A8A0 = 45;
    word_A83A = 0;
    word_A7FC = 0;
    word_A84A = 0;
    word_C0AC = 0;
    for (index = 0; index < 16 && word_A8A0 != 1; index += 1) {
        sub_4CF8();
    }
    CHECK(word_A8A0 == 1);
    CHECK(GetTickCount() - began < 1000U);
    CHECK(sound_calls == 1);
    CHECK((DWORD)sound_thread != GetCurrentThreadId());
    sub_4210(110, 0U, 0);
    EnterCriticalSection(&scmpoo_sound_lock);
    CHECK(scmpoo_pending_sound.kind == 1);
    CHECK(scmpoo_pending_sound.resource_id == 110);
    LeaveCriticalSection(&scmpoo_sound_lock);
    SetEvent(release_sound);
    CHECK(WaitForSingleObject(sound_entered, 2000U) == WAIT_OBJECT_0);
    CHECK(sound_calls == 2);
    CHECK(sound_resource == 110);
    scmpoo_shutdown_sound();
    CHECK(WaitForSingleObject(sound_stopped, 2000U) == WAIT_OBJECT_0);
}

int main(void)
{
    test_chime_cancellation();
    test_chime_tick_wrap();
    test_zero_coordinate_peer();
    test_animation_during_blocked_audio();
    if (failures == 0) {
        puts("Animation, chime, peer and blocked-audio regression checks passed.");
    }
    return failures != 0;
}
