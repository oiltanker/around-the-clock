# Around The Clock — store descriptions

Source-of-truth texts for the two workshop pages. Update both together.

## Steam Workshop (BBCode)

Item 3841078630 — https://steamcommunity.com/sharedfiles/filedetails/?id=3841078630
(game's manifest sends plain text; paste this on the item page)

```
[h1]Around The Clock[/h1]
A spin-off of the popular Second Shift mod, focused on the smallest factories: the ones powered by a single beaver.

[h2]Features[/h2]
[list]
[*]Shift mode selector — choose Regular, Two Shifts, or Three Shifts for each workplace
[*]Per-worker shifts — split workers between shifts and set individual shift hours
[*]Walker-powered generators scale their output with the number of active shifts
[*]Housing automatically pairs its sleep windows with the shifts of its residents
[*]Old saves load without issues
[/list]

[h2]Shift hours[/h2]
[b]Two shifts:[/b] first 6–18, second 18–6 (12h each)
[b]Three shifts:[/b] 0–8, 8–16, 16–24 (8h each)
Each resident's sleep window adapts so every shift gets at least 6 hours of overlap with home.

[h2]Dependencies[/h2]
[url=https://steamcommunity.com/sharedfiles/filedetails/?id=3284904751]Harmony[/url]

[h2]Credits[/h2]
Based on [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3614598709]Second Shift[/url] by eMka. This mod was made with permission and is a fork of its codebase.
```

## mod.io (sanitized HTML)

Paste as source on https://mod.io/g/timberborn — editor regenerates
data-source-hash, so omit it. Allowed tags: h1–h6, p, br, hr, ul/ol/li,
blockquote, pre, strong/b, em/i, u, s, code, sub/sup, a (title+href).

```html
<h1 dir="auto">Around The Clock</h1>
<p>A spin-off of the popular Second Shift mod, focused on the smallest factories: the ones powered by a single beaver.</p>
<h2 dir="auto">Features</h2>
<ul>
<li>Shift mode selector — choose Regular, Two Shifts, or Three Shifts for each workplace</li>
<li>Per-worker shifts — split workers between shifts and set individual shift hours</li>
<li>Walker-powered generators scale their output with the number of active shifts</li>
<li>Housing automatically pairs its sleep windows with the shifts of its residents</li>
<li>Old saves load without issues</li>
</ul>
<h2 dir="auto">Shift hours</h2>
<p><strong>Two shifts:</strong> first 6–18, second 18–6 (12h each)<br>
<strong>Three shifts:</strong> 0–8, 8–16, 16–24 (8h each)<br>
Each resident's sleep window adapts so every shift gets at least 6 hours of overlap with home.</p>
<h2 dir="auto">Dependencies</h2>
<p><a title="Harmony" href="https://mod.io/g/timberborn/m/harmony">Harmony</a></p>
<h2 dir="auto">Credits</h2>
<p>Based on <a title="Second Shift" href="https://mod.io/g/timberborn/m/second-shift">Second Shift</a> by eMka. This mod was made with permission and is a fork of its codebase.</p>
```

## Changelog

Full history lives in CHANGES.md; the short player-facing entry per release
goes into the Steam item's changelog field and the mod.io changelog box.
