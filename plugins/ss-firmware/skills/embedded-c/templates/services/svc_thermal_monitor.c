#include "svc_thermal_monitor.h"

fw_status_t thermal_monitor_init(thermal_monitor_t *self, const thermal_monitor_config_t *cfg)
{
    if (self == NULL || cfg == NULL || cfg->sensor == NULL || cfg->on_event == NULL ||
        cfg->hysteresis_milli_c < 0 || cfg->fault_limit == 0u) {
        return FW_ERR_INVALID_ARG;
    }

    self->cfg = *cfg;
    self->state = THERMAL_STATE_NORMAL;
    self->failed_reads = 0u;
    self->fault_reported = false;
    return FW_OK;
}

/* A failed read is counted; the fault is reported once, when the limit is reached. */
static void thermal_monitor_on_read_failed(thermal_monitor_t *self)
{
    if (self->failed_reads < UINT8_MAX) {
        self->failed_reads++;
    }
    if (self->failed_reads >= self->cfg.fault_limit && !self->fault_reported) {
        self->fault_reported = true;
        self->cfg.on_event(self->cfg.ctx, THERMAL_EVENT_SENSOR_FAULT, 0);
    }
}

static void thermal_monitor_on_sample(thermal_monitor_t *self, int32_t milli_c)
{
    self->failed_reads = 0u;
    self->fault_reported = false;

    switch (self->state) {
    case THERMAL_STATE_NORMAL:
        if (milli_c >= self->cfg.high_milli_c) {
            self->state = THERMAL_STATE_OVER_TEMP;
            self->cfg.on_event(self->cfg.ctx, THERMAL_EVENT_OVER_TEMP, milli_c);
        }
        break;
    case THERMAL_STATE_OVER_TEMP:
        if (milli_c <= self->cfg.high_milli_c - self->cfg.hysteresis_milli_c) {
            self->state = THERMAL_STATE_NORMAL;
            self->cfg.on_event(self->cfg.ctx, THERMAL_EVENT_RECOVERED, milli_c);
        }
        break;
    default:
        /* Unreachable unless the object is corrupted: fall back to a known state. */
        self->state = THERMAL_STATE_NORMAL;
        break;
    }
}

fw_status_t thermal_monitor_poll(thermal_monitor_t *self)
{
    int32_t milli_c = 0;
    fw_status_t st;

    if (self == NULL) {
        return FW_ERR_INVALID_ARG;
    }

    st = sensor_read_milli_c(self->cfg.sensor, &milli_c);
    if (st != FW_OK) {
        thermal_monitor_on_read_failed(self);
        return st;
    }

    thermal_monitor_on_sample(self, milli_c);
    return FW_OK;
}

thermal_state_t thermal_monitor_state(const thermal_monitor_t *self)
{
    return (self != NULL) ? self->state : THERMAL_STATE_NORMAL;
}
