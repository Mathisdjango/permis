#include "engineSound.h"
#include <math.h>
#include <stdlib.h>
#include <time.h>

#define SAMPLE_RATE 44100
#define PI 3.14159265358979323846

void engine_sound_init(EngineSound *es) {
    srand((unsigned int)time(NULL));
    es->rpm = 0.0f;
    es->throttle = 0.0f;
    es->volume = 0.0f;
    es->masterVolume = 0.8f;
    es->phase = 0.0;
    es->phase_crank = 0.0;
    es->current_rpm = 0.0f;
    es->target_rpm = 0.0f;
    es->is_cranking = 0;
    es->is_running = 0;
    es->startup_time = -1.0;
}

void engine_sound_update(EngineSound *es, float time, float dt) {
    /* Startup sequence matching b2b.fs timing */
    if (time > 14.0f && !es->is_cranking && !es->is_running) {
        es->is_cranking = 1;
        es->startup_time = time;
    }

    if (es->is_cranking) {
        float elapsed = time - es->startup_time;
        if (elapsed < 1.5f) {
            /* Cranking sound: periodic pulses */
            es->target_rpm = 0.05f;
            es->volume = 0.3f;
        } else {
            /* Ignition! */
            es->is_cranking = 0;
            es->is_running = 1;
            es->target_rpm = 0.3f; /* Flare up on start */
        }
    }

    if (es->is_running) {
        float elapsed = time - es->startup_time;
        if (elapsed < 3.0f) {
            /* Settling to idle */
            es->target_rpm = 0.15f + 0.15f * expf(-(elapsed - 1.5f) * 2.0f);
        } else if (time < 18.0f) {
            /* Idle */
            es->target_rpm = 0.15f;
        } else if (time < 22.0f) {
            /* Acceleration */
            float t = (time - 18.0f) / 4.0f;
            es->target_rpm = 0.15f + t * 0.65f;
            es->throttle = t;
        } else if (time < 68.0f) {
            /* Cruising */
            es->target_rpm = 0.8f + 0.05f * sinf(time * 0.5f);
            es->throttle = 0.5f;
        } else {
            /* Crash / Engine kill */
            es->target_rpm *= 0.95f;
            es->volume *= 0.9f;
            if (es->volume < 0.01f) es->is_running = 0;
        }
        es->volume = 0.6f + es->target_rpm * 0.4f;
    }

    /* Smooth RPM transition */
    es->current_rpm += (es->target_rpm - es->current_rpm) * 5.0f * dt;
    es->rpm = es->current_rpm;
}

static float noise() {
    return (float)rand() / (float)RAND_MAX * 2.0f - 1.0f;
}

void engine_sound_mix(EngineSound *es, Uint8 *stream, int len) {
    if (es->volume <= 0.0f && !es->is_cranking) return;

    Sint16 *buffer = (Sint16 *)stream;
    int samples = len / 2; /* 16-bit samples */
    
    /* Base frequency based on RPM */
    /* Idle ~ 800 RPM -> ~13 Hz fundamental */
    /* Max ~ 7000 RPM -> ~116 Hz fundamental */
    float base_freq = 15.0f + es->rpm * 100.0f;
    
    for (int i = 0; i < samples; i += 2) { /* Stereo */
        double dt = 1.0 / SAMPLE_RATE;
        
        if (es->is_cranking) {
            /* Periodic "chug" during cranking */
            es->phase_crank += 2.0 * PI * 6.0 * dt;
            float crank_mod = powf(0.5f + 0.5f * sinf(es->phase_crank), 10.0f);
            float s = noise() * crank_mod * 0.5f;
            
            Sint16 val = (Sint16)(s * 10000.0f * es->masterVolume);
            buffer[i] = (Sint16)fmaxf(-32768, fminf(32767, buffer[i] + val));
            buffer[i+1] = (Sint16)fmaxf(-32768, fminf(32767, buffer[i+1] + val));
            continue;
        }

        if (!es->is_running) continue;

        es->phase += 2.0 * PI * base_freq * dt;
        if (es->phase > 2.0 * PI) es->phase -= 2.0 * PI;

        /* Synthesis:
           1. Fundamental thumping (sub-harmonics of cylinder fires)
           2. Cylinder fire pulses (rich in harmonics)
           3. High frequency noise (exhaust/intake)
        */

        /* Cylinder fires (4 fires per 2 revolutions for 4-cylinder, let's say 4 per revolution for beefiness) */
        float cylinder_fire = powf(0.5f + 0.5f * sinf(es->phase * 4.0), 4.0f);
        float intake_noise = noise() * es->throttle * 0.2f;
        
        /* Fundamental and harmonics */
        float s = 0.4f * sinf(es->phase);
        s += 0.3f * sinf(es->phase * 2.0);
        s += 0.2f * sinf(es->phase * 3.0);
        s += 0.1f * sinf(es->phase * 0.5); /* Sub-harmonic */
        
        /* Mix in cylinder pulses and noise */
        s = s * 0.4f + cylinder_fire * (0.6f + intake_noise);
        
        /* Apply some distortion for "growl" */
        s = tanhf(s * (1.2f + es->throttle));

        Sint16 val = (Sint16)(s * 8000.0f * es->volume * es->masterVolume);
        
        /* Mix into existing stream (music) */
        buffer[i] = (Sint16)fmaxf(-32768, fminf(32767, buffer[i] + val));
        buffer[i+1] = (Sint16)fmaxf(-32768, fminf(32767, buffer[i+1] + val));
    }
}
