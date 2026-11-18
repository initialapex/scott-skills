/*
 * svc_thermal_monitor.h - over-temperature detection with hysteresis (service layer).
 *
 * Shows four habits at once:
 *   - it depends on the sensor INTERFACE only (injected), never on a chip driver;
 *   - it has no thread, timer, or delay of its own: the caller runs thermal_monitor_poll()
 *     from a task, a timer callback, or the main loop, which keeps it testable;
 *   - it reports upward through a callback with a context pointer;
 *   - its state is an explicit enum updated in one place.
 */
#ifndef SVC_THERMAL_MONITOR_H
#define SVC_THERMAL_MONITOR_H

#include "fw_common.h"
#include "sensor.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef enum thermal_event {
    THERMAL_EVENT_OVER_TEMP,   /* temperature rose to or above the high limit     */
    THERMAL_EVENT_RECOVERED,   /* temperature fell to the high limit minus hyst.  */
    THERMAL_EVENT_SENSOR_FAULT /* fault_limit reads in a row failed               */
} thermal_event_t;

/*
 * Called from the context that calls thermal_monitor_poll(), never from an ISR.
 * value_milli_c is the last good reading; for THERMAL_EVENT_SENSOR_FAULT it is 0.
 */
typedef void (*thermal_event_cb_t)(void *ctx, thermal_event_t event, int32_t value_milli_c);

typedef struct thermal_monitor_config {
    sensor_t *sensor;            /* required                                          */
    int32_t high_milli_c;        /* alarm when the reading reaches this value          */
    int32_t hysteresis_milli_c;  /* must be >= 0; clear when reading <= high - hyst    */
    uint8_t fault_limit;         /* consecutive failed reads before SENSOR_FAULT; >= 1 */
    thermal_event_cb_t on_event; /* required                                          */
    void *ctx;                   /* passed back to on_event unchanged                 */
} thermal_monitor_config_t;

typedef enum thermal_state {
    THERMAL_STATE_NORMAL,
    THERMAL_STATE_OVER_TEMP
} thermal_state_t;

typedef struct thermal_monitor {
    thermal_monitor_config_t cfg;
    thermal_state_t state;
    uint8_t failed_reads;
    bool fault_reported;
} thermal_monitor_t;

fw_status_t thermal_monitor_init(thermal_monitor_t *self, const thermal_monitor_config_t *cfg);

/* Take one sample and run the state machine. Returns the status of the sensor read. */
fw_status_t thermal_monitor_poll(thermal_monitor_t *self);

thermal_state_t thermal_monitor_state(const thermal_monitor_t *self);

#ifdef __cplusplus
}
#endif

#endif /* SVC_THERMAL_MONITOR_H */
