#include "Window.h"
#include <utility/Log.h>
#include <windows.h>

namespace ojgl {

class Window::Details {
public:
    Details(unsigned width, unsigned height)
        : _width(width)
        , _height(height)
    {
    }
    HWND CreateOpenGLWindow(const char* title, int x, int y, BYTE type, DWORD flags, bool fullScreen);
    HWND CreateFullscreenWindow(HWND hwnd, HINSTANCE hInstance);
    static LONG WINAPI WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

    HDC _hDC; // device context
    HGLRC _hRC; // opengl context
    HWND _hWnd; // window
    MSG _msg; // message
    unsigned _width;
    unsigned _height;
    ojstd::vector<UINT> _keys;
    bool _closed;
};

Window::Window(unsigned width, unsigned height, bool fullScreen)
    : _priv(ojstd::make_shared<Details>(width, height))
{
    ShowCursor(false);

    _priv->_hWnd = _priv->CreateOpenGLWindow("Eldur - OJ", 0, 0, PFD_TYPE_RGBA, 0, fullScreen);
    if (_priv->_hWnd == nullptr) {
        exit(1);
    }

    _priv->_hDC = GetDC(_priv->_hWnd);
    _priv->_hRC = wglCreateContext(_priv->_hDC);
    wglMakeCurrent(_priv->_hDC, _priv->_hRC);
    ShowWindow(_priv->_hWnd, 1);

    Window* pThis = this;
    SetLastError(0);
    if (!SetWindowLongPtr(_priv->_hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pThis))) {
        if (GetLastError() != 0) {
            LOG_ERROR("SetWindowLongPtr failed in Window");
        }
    }
}

Window::~Window()
{
    wglMakeCurrent(nullptr, nullptr);
    ReleaseDC(_priv->_hWnd, _priv->_hDC);
    wglDeleteContext(_priv->_hRC);
    DestroyWindow(_priv->_hWnd);
}

void Window::getMessages()
{
    while (PeekMessage(&_priv->_msg, nullptr, 0, 0, PM_REMOVE)) {
        TranslateMessage(&_priv->_msg);
        DispatchMessage(&_priv->_msg);
    }
}

ojstd::vector<unsigned int> Window::getPressedKeys()
{
    auto keys = this->_priv->_keys;
    this->_priv->_keys.clear();
    return keys;
}

HWND Window::Details::CreateFullscreenWindow(HWND hwnd, HINSTANCE hInstance)
{
    HMONITOR hmon = MonitorFromWindow(hwnd,
        MONITOR_DEFAULTTONEAREST);
    MONITORINFO mi = { sizeof(mi) };
    if (!GetMonitorInfo(hmon, &mi)) {
        return nullptr;
    }
    return CreateWindow(TEXT("static"),
        TEXT("something interesting might go here"),
        WS_POPUP | WS_VISIBLE,
        mi.rcMonitor.left,
        mi.rcMonitor.top,
        mi.rcMonitor.right - mi.rcMonitor.left,
        mi.rcMonitor.bottom - mi.rcMonitor.top,
        hwnd, nullptr, hInstance, nullptr);
}

HWND Window::Details::CreateOpenGLWindow(const char* title, int x, int y, BYTE type, DWORD flags, bool fullScreen)
{
    int pf;
    HDC hDC;
    HWND hWnd;
    WNDCLASS wc;
    PIXELFORMATDESCRIPTOR pfd;
    static HINSTANCE hInstance = nullptr;

    /* only register the window class once - use hInstance as a flag. */
    if (!hInstance) {
        hInstance = GetModuleHandle(NULL);
        wc.style = CS_OWNDC;
        wc.lpfnWndProc = (WNDPROC)WindowProc;
        wc.cbClsExtra = 0;
        wc.cbWndExtra = 0;
        wc.hInstance = hInstance;
        wc.hIcon = LoadIcon(NULL, IDI_WINLOGO);
        wc.hCursor = LoadCursor(NULL, IDC_ARROW);
        wc.hbrBackground = NULL;
        wc.lpszMenuName = NULL;
        wc.lpszClassName = "OpenGL"; //L"OpenGL";

        if (!RegisterClass(&wc)) {
            MessageBox(NULL, "RegisterClass() failed:  "
                             "Cannot register window class.",
                "Error", MB_OK);
            return NULL;
        }
    }

    hWnd = CreateWindow("OpenGL", title, WS_OVERLAPPEDWINDOW | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
        x, y, this->_width, this->_height, NULL, NULL, hInstance, NULL);
    if (fullScreen) {
        hWnd = CreateFullscreenWindow(hWnd, hInstance);
    }

    if (hWnd == NULL) {
        MessageBox(NULL, "CreateWindow() failed:  Cannot create a window.",
            "Error", MB_OK);
        return NULL;
    }

    hDC = GetDC(hWnd);

    /* there is no guarantee that the contents of the stack that become
		the pfd are zeroed, therefore _make sure_ to clear these bits. */
    memset(&pfd, 0, sizeof(pfd));
    pfd.nSize = sizeof(pfd);
    pfd.nVersion = 1;
    pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | flags;
    pfd.iPixelType = type;
    pfd.cColorBits = 32;

    pf = ChoosePixelFormat(hDC, &pfd);
    if (pf == 0) {
        MessageBox(NULL, "ChoosePixelFormat() failed:  "
                         "Cannot find a suitable pixel format.",
            "Error", MB_OK);
        return 0;
    }

    if (SetPixelFormat(hDC, pf, &pfd) == FALSE) {
        MessageBox(NULL, "SetPixelFormat() failed:  "
                         "Cannot set format specified.",
            "Error", MB_OK);
        return 0;
    }

    DescribePixelFormat(hDC, pf, sizeof(PIXELFORMATDESCRIPTOR), &pfd);

    ReleaseDC(hWnd, hDC);

    return hWnd;
}

LONG WINAPI Window::Details::WindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    Window* pThis = reinterpret_cast<Window*>(GetWindowLongPtr(hWnd, GWLP_USERDATA));

    static PAINTSTRUCT ps;

    switch (uMsg) {
    case WM_PAINT:
        BeginPaint(hWnd, &ps);
        EndPaint(hWnd, &ps);
        return 0;

    case WM_SIZE:
        // glViewport(0, 0, LOWORD(lParam), HIWORD(lParam));
        PostMessage(hWnd, WM_PAINT, 0, 0);
        return 0;

    case WM_CHAR:
        switch (wParam) {
        case 27: /* ESC key */
            PostQuitMessage(0);
            break;
        }
        return 0;
    case WM_KEYUP:
        if (pThis) {
            pThis->_priv->_keys.push_back(wParam);
        }
        return 0;
    case WM_CLOSE:
        PostQuitMessage(0);
        return 0;
    }

    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}
} // namespace ojgl
