#pragma once
//#include <memory>
#include "utility/OJstd.h"

namespace ojgl {
class Texture {
public:
    static ojstd::shared_ptr<Texture> construct(int width, int height, int channels, unsigned char* img);
    ~Texture();
    unsigned textureID();

private:
    void load(unsigned char* img);
    Texture(int width, int height, int channels, unsigned char* img);

    const int _width;
    const int _height;
    const int _channels;
    unsigned _textureID;
};
} //namespace ojgl