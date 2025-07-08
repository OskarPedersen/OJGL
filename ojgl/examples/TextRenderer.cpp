#include "TextRenderer.hpp"
#include "utility/Log.h"
#include <Windows.h>

using namespace ojgl;

TextRenderer& TextRenderer::instance()
{
    static TextRenderer instance;
    return instance;
}

void TextRenderer::setHDC(void* hdc)
{
    _hdcBackend = hdc;
}

ojstd::shared_ptr<Texture> TextRenderer::get(const ojstd::string& text, const ojstd::string& font)
{
    int fontSize = 96;
    HDC vhdc = CreateCompatibleDC((HDC)_hdcBackend);
    auto hFont = CreateFont(fontSize, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_OUTLINE_PRECIS,
        CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, VARIABLE_PITCH, TEXT(font.c_str()));

    SelectObject(vhdc, hFont);

    int totalWidth = 3;
    for (size_t i = 0; i < static_cast<size_t>(text.length()); i++) {
        const char* ptr = text.c_str();
        int res[1];
        GetCharWidth32A(vhdc, ptr[i], ptr[i], res);
        totalWidth += res[0];
    }

    int w = totalWidth;
    int h = fontSize;

    RECT rect { 0, 0, w, h };

    _ASSERT_EXPR(vhdc, "error with vhdc");

    HBITMAP hbmp = CreateCompatibleBitmap((HDC)_hdcBackend, w, h);
    BITMAPINFO bmpi = { { sizeof(BITMAPINFOHEADER), w, h, 1, 32, BI_RGB, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0 } };

    SelectObject(vhdc, hbmp);
    ExtFloodFill(vhdc, 0, 0, 0x00FFFFFF, FLOODFILLBORDER);

    // SetTextAlign(vhdc, TA_CENTER);
    DrawText(vhdc, text.c_str(), text.length(), &rect, DT_CENTER);

    GetDIBits(vhdc, hbmp, 0, h, nullptr, &bmpi, BI_RGB);

    BYTE* buf = new BYTE[bmpi.bmiHeader.biSizeImage];
    GetDIBits(vhdc, hbmp, 0, h, buf, &bmpi, BI_RGB);

    auto result = Texture::construct(w, h, 4, buf);

    delete[] buf;

    return result;
}
