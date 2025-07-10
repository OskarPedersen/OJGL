#pragma once

#include "utility/OJstd.h"
namespace ojgl::resources {
// clang-format off
  ojstd::vector<ojstd::Pair<ojstd::string, ojstd::string>> shaders = {
          {
  #include "shaders/common/passThrough.fs"
          , "common/passThrough.fs"
          },
          {
  #include "shaders/common/noise.fs"
          , "common/noise.fs"
          },

          {
  #include "shaders/common/primitives.fs"
          , "common/primitives.fs"
          },

          {
  #include "shaders/common/raymarch_settings.fs"
          , "common/raymarch_settings.fs"
          },

          {
  #include "shaders/common/raymarch_utils.fs"
          , "common/raymarch_utils.fs"
          },

          {
  #include "shaders/common/utils.fs"
          , "common/utils.fs"
          },

          {
  #include "shaders/common/fxaa.fs"
          , "common/fxaa.fs"
          },

          {
  #include "shaders/common/fxaa.vs"
          , "common/fxaa.vs"
          },

          {
  #include "shaders/common/quad.vs"
          , "common/quad.vs"
          },

          {
#include "shaders/common/radial_blur.fs"
          , "common/radial_blur.fs"
          },
          {
  #include "shaders/common/fade.fs"
          , "common/fade.fs"
          },
          {
#include "shaders/edison2022/blur1.fs"
          , "edison2022/blur1.fs"
          },
          {
#include "shaders/edison2025/ufo_scenes.fs"
          , "edison2025/ufo_scenes.fs"
          },
          {
#include "shaders/edison2025/noise.fs"
          , "edison2025/noise.fs"
          },
          {
#include "shaders/edison2025/chrom_ab.fs"
          , "edison2025/chrom_ab.fs"
          },
          {
#include "shaders/edison2025/blur1.fs"
          , "edison2025/blur1.fs"
          },
          {
#include "shaders/edison2025/experiment.fs"
          , "edison2025/experiment.fs"
          },
          {
#include "shaders/edison2025/lissajous.fs"
          , "edison2025/lissajous.fs"
          },
          {
#include "shaders/edison2025/radar.fs"
          , "edison2025/radar.fs"
          },
          {
#include "shaders/edison2025/oj_text.fs"
          , "edison2025/oj_text.fs"
          },
          {
#include "shaders/edison2025/borgila.fs"
          , "edison2025/borgila.fs"
          },
          {
#include "shaders/edison2025/indoor.fs"
          , "edison2025/indoor.fs"
          },
          {
#include "shaders/edison2025/ufo_hyperspace.fs"
          , "edison2025/ufo_hyperspace.fs"
          },
          {
#include "shaders/edison2025/ufo_landing.fs"
          , "edison2025/ufo_landing.fs"
          },
          {
#include "shaders/edison2025/stars.fs"
          , "edison2025/stars.fs"
          }
  };
// clang-format on
}
