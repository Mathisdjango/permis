#ifndef ENGINE_SOUND_H
#define ENGINE_SOUND_H

#include <SDL.h>

typedef struct {
    float rpm;          /* 0.0 to 1.0 */
    float throttle;     /* 0.0 to 1.0 */
    float volume;       /* 0.0 to 1.0 */
    float masterVolume; /* 0.0 to 1.0 */
    
    /* Synthesis state */
    double phase;
    double phase_crank;
    float current_rpm;
    float target_rpm;
    
    int is_cranking;
    int is_running;
    double startup_time;
} EngineSound;

extern void engine_sound_init(EngineSound *es);
extern void engine_sound_update(EngineSound *es, float time, float dt);
extern void engine_sound_mix(EngineSound *es, Uint8 *stream, int len);

#endif
