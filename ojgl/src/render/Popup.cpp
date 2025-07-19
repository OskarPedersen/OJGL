#include "Popup.h"
#include "utility/OJstd.h"
#include <utility/Log.h>
#include <windows.h>

#ifndef UNICODE
#define UNICODE
#endif

using namespace ojgl;

namespace {

struct CallbackData {
    int reslistindex = 0;
#ifdef _DEBUG
    bool full = false;
#else
    bool full = true;
#endif
};

constexpr int PLAY = 1;
constexpr int QUIT = 2;
constexpr int FULL = 3;
constexpr int RESOLUTION = 4;

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    CallbackData* dataptr = reinterpret_cast<CallbackData*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));

    switch (uMsg) {
    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    case WM_CREATE: {

        break;
    }
    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC hdc = BeginPaint(hwnd, &ps);

        FillRect(hdc, &ps.rcPaint, (HBRUSH)(COLOR_WINDOW + 1));
        EndPaint(hwnd, &ps);
        break;
    }
    case WM_CTLCOLORSTATIC: {
        static HBRUSH hBrushColor;
        if (!hBrushColor) {
            hBrushColor = CreateSolidBrush(RGB(255, 255, 255));
            SetBkColor((HDC)wParam, RGB(255, 255, 255));
        }
        return (LRESULT)hBrushColor;
    }
    case WM_CLOSE:
        DestroyWindow(hwnd);
        //PostQuitMessage(0);
        exit(0);
    case WM_COMMAND:
        if (HIWORD(wParam) == BN_CLICKED && LOWORD(wParam) == PLAY) {
            LOG_INFO("Play button clicked");
            DestroyWindow(hwnd);
        }

        if (HIWORD(wParam) == BN_CLICKED && LOWORD(wParam) == QUIT) {
            LOG_INFO("Quit button clicked");
            exit(0);
        }

        if (HIWORD(wParam) == CBN_SELCHANGE && LOWORD(wParam) == RESOLUTION) {
            int index = SendMessage((HWND)lParam, (UINT)CB_GETCURSEL, (WPARAM)0, (LPARAM)0);
            LOG_INFO("Selected list item " << index);
            dataptr->reslistindex = index;
        }

        if (LOWORD(wParam) == FULL) {
            if (IsDlgButtonChecked(hwnd, FULL)) {
                LOG_INFO("Fullscreen unchecked");
                CheckDlgButton(hwnd, FULL, BST_UNCHECKED);
                dataptr->full = false;
            } else {
                LOG_INFO("Fullscreen checked");
                CheckDlgButton(hwnd, FULL, BST_CHECKED);
                dataptr->full = true;
            }
        }
        break;
    }
    return DefWindowProc(hwnd, uMsg, wParam, lParam);
}
}

popup::Data popup::show()
{
    static HINSTANCE hInstance = nullptr;

    hInstance = GetModuleHandle(NULL);

    // Register the window class.
    ojstd::string CLASS_NAME = "OJ Class";

    WNDCLASS wc = {};

    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = CLASS_NAME.c_str();

    RegisterClass(&wc);

    constexpr int width = 200;
    constexpr int height = 200;

    // Create the window.
    auto style = WS_OVERLAPPED | WS_SYSMENU;
    style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
    HWND hwnd = CreateWindowEx(0, CLASS_NAME.c_str(), "OJ - Aurora Borgilis",
        style, CW_USEDEFAULT, CW_USEDEFAULT, width, height,
        NULL, NULL, hInstance, NULL);

    HMONITOR hmon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
    MONITORINFO mi = { sizeof(mi) };
    if (!GetMonitorInfo(hmon, &mi)) {
        LOG_INFO("Can not retrieve monitor info.");
    }

    int defaultWidth = mi.rcMonitor.right - mi.rcMonitor.left;
    int defaultHeight = mi.rcMonitor.bottom - mi.rcMonitor.top;
    SetWindowPos(hwnd, HWND_TOP, defaultWidth / 2 - width / 2, defaultHeight / 2 - height / 2, width, height, SWP_SHOWWINDOW | SWP_NOZORDER);

    if (hwnd == NULL) {
        LOG_ERROR("hwnd == NULL");
        exit(0);
    }

    CallbackData callbackData;
    if (!SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(&callbackData))) {
        if (GetLastError() != 0) {
            LOG_ERROR("SetWindowLongPtr failed in popup");
        }
    }
    DEVMODE dm = { 0 };
    dm.dmSize = sizeof(dm);
    ojstd::vector<ojstd::Pair<int, int>> resolutions;
    ojstd::unordered_set<ojstd::Pair<int, int>> checkedResolutions;

    for (int iModeNum = 0; EnumDisplaySettings(NULL, iModeNum, &dm) != 0; iModeNum++) {
        ojstd::Pair<int, int> res(dm.dmPelsWidth, dm.dmPelsHeight);
        if (checkedResolutions.find(res) == checkedResolutions.end()) {
            resolutions.push_back(res);
            checkedResolutions.insert(res);
        }
        LOG_INFO("Mode #" << iModeNum << " = " << dm.dmPelsWidth << "x" << dm.dmPelsHeight << " dmBitsPerPel " << dm.dmBitsPerPel << " dmDisplayFrequency " << dm.dmDisplayFrequency);
    }

    for (int i = 0; i < resolutions.size(); i++) {
        LOG_INFO("Res " << resolutions[i].first << "x" << resolutions[i].second);
    }

    //HWND checkbox = CreateWindowEx(NULL, CLASS_NAME.c_str(), "Checkbox", BS_CHECKBOX, 50, 50, 50, 50, hwnd, NULL, NULL, NULL);

    CreateWindow(TEXT("button"), TEXT("Play"),
        WS_VISIBLE | WS_CHILD,
        10, 100, 60, 25,
        hwnd, (HMENU)PLAY, NULL, NULL);

    CreateWindow(TEXT("button"), TEXT("Quit"),
        WS_VISIBLE | WS_CHILD,
        100, 100, 60, 25,
        hwnd, (HMENU)QUIT, NULL, NULL);

    HWND fullscreenHWND = CreateWindow(TEXT("BUTTON"), "Fullscreen", BS_CHECKBOX | WS_VISIBLE | WS_CHILD, 10, 50, 150, 40, hwnd, (HMENU)FULL, NULL, NULL);

    SendMessage(fullscreenHWND, BM_SETCHECK, callbackData.full ? BST_CHECKED : BST_UNCHECKED, 0);

    HWND hWndComboBox = CreateWindow("ComboBox", TEXT(""),
        CBS_DROPDOWN | CBS_HASSTRINGS | WS_CHILD | WS_OVERLAPPED | WS_VISIBLE | WS_VSCROLL,
        10, 20, 150, 150, hwnd, (HMENU)RESOLUTION, NULL, // HINST_THISCOMPONENT
        NULL);

    for (int i = 0; i < resolutions.size(); ++i) {
        ojstd::string res;
        res.append(ojstd::to_string(resolutions[i].first));
        res.append("x");
        res.append(ojstd::to_string(resolutions[i].second));
        // Add string to combobox.
        SendMessage(hWndComboBox, (UINT)CB_ADDSTRING, (WPARAM)0, (LPARAM)res.c_str());
    }

    // Send the CB_SETCURSEL message to display an initial item
    //  in the selection field
    auto pred = [defaultWidth, defaultHeight](auto p) { return p.first == defaultWidth && p.second == defaultHeight; };
    int index = ojstd::find_if(resolutions.begin(), resolutions.end(), pred) - resolutions.begin();

#ifdef _DEBUG
    const ojstd::string registrySubkey = "Software\\OJ";
    const ojstd::string registryKey = "resolution_index";

    // Try to read the last used resolution index from the registry
    {
        DWORD savedIndex;
        DWORD type;
        DWORD cbData = sizeof(savedIndex);
        const LSTATUS status = RegGetValueA(
            HKEY_CURRENT_USER,
            registrySubkey.c_str(),
            registryKey.c_str(),
            RRF_RT_ANY,
            &type,
            &savedIndex,
            &cbData);

        if (status == ERROR_SUCCESS) {
            _ASSERTE(type == REG_DWORD);
            index = static_cast<int>(savedIndex);
        } else {
            _ASSERTE(status == ERROR_FILE_NOT_FOUND);
        }
    }
#endif //  _DEBUG

    _ASSERTE(index < resolutions.size());
    if (index >= resolutions.size())
        index = 0;
    SendMessage(hWndComboBox, CB_SETCURSEL, (WPARAM)index, (LPARAM)0);
    callbackData.reslistindex = index;

    ShowWindow(hwnd, 1);

    // Run the message loop.

    MSG msg = {};
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
    popup::Data data;
    data.full = callbackData.full;
    data.width = resolutions[callbackData.reslistindex].first;
    data.height = resolutions[callbackData.reslistindex].second;

#ifdef _DEBUG
    {
        HKEY keyHandle;

        // Create key if it doesn't exist, otherwise open it
        {
            DWORD disposition;
            const LSTATUS status = RegCreateKeyExA(
                HKEY_CURRENT_USER,
                registrySubkey.c_str(),
                0,
                NULL,
                REG_OPTION_NON_VOLATILE,
                KEY_ALL_ACCESS,
                NULL,
                &keyHandle,
                &disposition);
            _ASSERTE(status == ERROR_SUCCESS || disposition == REG_OPENED_EXISTING_KEY);
        }

        // Save the selected resolution index into the registry
        {
            const DWORD resolutionIndex = static_cast<DWORD>(callbackData.reslistindex);
            const LSTATUS status = RegSetValueExA(
                keyHandle,
                registryKey.c_str(),
                0,
                REG_DWORD,
                (const BYTE*)&resolutionIndex,
                sizeof(DWORD));
            _ASSERTE(status == ERROR_SUCCESS);
        }

        RegCloseKey(keyHandle);
    }
#endif //  _DEBUG

    return data;
}
