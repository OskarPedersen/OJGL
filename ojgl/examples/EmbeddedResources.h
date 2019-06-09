#pragma once

#include "utility/OJstd.h"
namespace ojgl {

namespace resources {

    namespace fragment {

        ojstd::string post{
#include "shaders/post.fs"
        };

        ojstd::string fxaa{
#include "shaders/fxaa.fs"
        };

        ojstd::string edison_1{
#include "shaders/edison_1.fs"
        };

        ojstd::string edison_2{
#include "shaders/edison_1.fs"
        };

        ojstd::string edison_1_post{
#include "shaders/edison_1_post.fs"
        };

        ojstd::string planets{
#include "shaders/planets.fs"
        };

    } //namespace fragment

    namespace vertex {

        ojstd::string edison{
#include "shaders/edison.vs"
        };
        ojstd::string fxaa{
#include "shaders/fxaa.vs"
        };
        ojstd::string post{
#include "shaders/post.vs"
        };

    } //namespace vertex

    namespace songs {

        unsigned char song[] = {
#include "songs/song.inc"
        };
    } //namespace songs
}
}
