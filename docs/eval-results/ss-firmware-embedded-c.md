# ss-firmware:embedded-c eval results

Run with `claude plugin eval plugins/ss-firmware` on 2026-11-21 to 2026-11-23.
Plugin version 0.1.0. Agent model: claude-opus-5-5. Each case runs with the plugin
and against a no-plugin baseline (ablation `with-without`). Score is the share of
graders passed (all graders weigh 1); a run counts as passed only when every grader passes.

Runs used `--scaffold --allow-tools Write Edit`. Bash is not granted because this Windows
machine has no sandbox backend, so every run with a Bash grant is refused.

## Scores

| Case | Runs per arm | Judge | With plugin | Without | Delta |
|---|---|---|---|---|---|
| esp32-layered-sensor-host-test | 3 | haiku | 0.89 | 0.63 | +0.26 |
| fx2-oop-on-8051 | 3 | haiku | 0.89 | 0.63 | +0.26 |
| fx3-slavefifo-stream | 3 | haiku | 0.79 | 0.29 | +0.50 |
| stm32-uart-refactor | 2 | sonnet | 0.94 | 0.81 | +0.125 |

Scores from different judges are not directly comparable. The haiku judge was noisy on
stm32 (same answer, votes split): with haiku the delta was 0, with sonnet it is +0.125.
The esp32, fx2 and fx3 cases have not been re-judged with sonnet.

## Graders that failed with the plugin

| Case | Grader | Failed in |
|---|---|---|
| esp32 | 08-bounded-bus-waits | 2 of 3 runs |
| esp32 | 03-service-callback-has-context-pointer | 1 of 3 |
| fx2 | 05-isr-sets-flag-main-handles-sudav | 3 of 3 (also fails without the plugin) |
| fx3 | 03-buffer-size-and-alignment | 3 of 3 (also fails without the plugin) |
| fx3 | 04-idempotent-stream-stop-and-teardown | 2 of 3 |
| stm32 | 02-flags-line-buffer-overflow | 1 of 2 |

Not yet checked whether these are gaps in the skill or graders that are too strict.

## Cost

About 51 USD in total over all attempts, including failed and partial runs. By attempt:
first pass (esp32, fx2, part of fx3) 14.0, fx3 rerun 8.98, stm32 aborted by the usage limit 2.69,
stm32 with haiku 5.35, stm32 with sonnet cut by the cost ceiling 9.44, stm32 with sonnet
complete 10.90 USD. Sonnet as judge costs roughly 3 to 4 USD
per with-plugin run, haiku far less.

## Notes

- Raw results (`plugins/ss-firmware/evals/results/`) are not committed; the folder is
  git-ignored because it is large (about 27 MB for these runs) and would be packed into the
  plugin zip.
- Runs that hit the 5-hour usage limit or the cost ceiling are not scored; a 0 in such a
  report means not judged, not a real score.
