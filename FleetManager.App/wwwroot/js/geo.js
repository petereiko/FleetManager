
        (function ($) {
          if (!window.jQuery) {
            console.warn('jQuery not found — geo handler not installed.');
        return;
          }

        // Diagnostic marker (so your console checks return true)
        window.__geoHandlerInstalled = true;
        console.log('Geo delegated handler installed');

        function findScoped($btn, selector) {
            var $modalBody = $btn.closest('.modal-body');
        if ($modalBody.length) {
              var $found = $modalBody.find(selector);
        if ($found.length) return $found;
            }
        return $(selector);
          }

        function setSpinner($btn, on) {
            var $spinner = $btn.find('.geo-spinner');
        if ($spinner.length) $spinner.toggleClass('visually-hidden', !on);
        $btn.prop('disabled', !!on);
          }
        function setStatus($btn, text, isError) {
            var $s = findScoped($btn, '.geo-status');
        if ($s.length) {$s.text(text).toggleClass('text-danger', !!isError).toggleClass('text-muted', !isError); } else console.log('[geo] status', text);
          }
        function setAccuracy($btn, text, isErr) {
            var $b = findScoped($btn, '.geo-accuracy-badge');
        if ($b.length) {$b.text(text).toggleClass('bg-danger', !!isErr).toggleClass('bg-secondary', !isErr); } else console.log('[geo] acc', text);
          }
        function setAddress($btn, text) {
            var $a = findScoped($btn, '.geo-address');
        if ($a.length) $a.text(text);
          }

        async function reverseGeocode(lat, lon) {
            try {
              var url = 'https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=' + encodeURIComponent(lat) + '&lon=' + encodeURIComponent(lon);
        const resp = await fetch(url, {headers: {'Accept': 'application/json' }});
        if (!resp.ok) return null;
        const data = await resp.json();
        return data.display_name || null;
            } catch (e) {console.warn('reverse geocode error', e); return null; }
          }

        function onGeoSuccess($btn, pos) {
            try {
              var lat = pos.coords.latitude, lon = pos.coords.longitude, acc = pos.coords.accuracy;
        var $lat = findScoped($btn, '.geo-lat'), $lon = findScoped($btn, '.geo-lon'), $acc = findScoped($btn, '.geo-acc');
        if ($lat.length) $lat.val(lat);
        if ($lon.length) $lon.val(lon);
        if ($acc.length) $acc.val(acc);
        setAccuracy($btn, '±' + Math.round(acc) + ' m', false);
        setStatus($btn, 'Location captured', false);
              reverseGeocode(lat, lon).then(addr => { if (addr) setAddress($btn, addr); });
        console.log('[geo] success', {lat, lon, acc});
            } finally {setSpinner($btn, false); }
          }

        function onGeoError($btn, err) {
            setSpinner($btn, false);
        let msg = 'Could not get location';
        if (err && typeof err.code !== 'undefined') {
              if (err.code === 1) msg = 'Permission denied. Allow location and retry.';
        else if (err.code === 2) msg = 'Position unavailable. Try again outdoors.';
        else if (err.code === 3) msg = 'Timeout. Try again.';
            }
        setStatus($btn, msg, true);
        setAccuracy($btn, 'No location', true);
        console.warn('[geo] error', err);
          }

        function attemptGeolocation($btn) {
            if (!navigator.geolocation) {setStatus($btn, 'Geolocation not supported', true); return; }
        setSpinner($btn, true);
        setStatus($btn, 'Requesting location…', false);
        setAccuracy($btn, 'Locating…', false);
        navigator.geolocation.getCurrentPosition(
        function (pos) {onGeoSuccess($btn, pos); },
        function (err) {onGeoError($btn, err); },
        {enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
        );
          }

        // delegated click handler (works for AJAX-loaded partials)
        $(document).on('click', '.geo-fill-btn, #fillLocationBtn', function (e) {
            e.preventDefault();
        var $btn = $(this);
        attemptGeolocation($btn);
          });

        // optional: auto-trigger when modal opens and element with data-auto-fill="geo" exists
        $(document).on('shown.bs.modal', '.modal', function () {
            var $modal = $(this);
        if ($modal.find('[data-auto-fill="geo"]').length) {
              var $btn = $modal.find('.geo-fill-btn, #fillLocationBtn').first();
        if ($btn && $btn.length) setTimeout(function () {$btn.trigger('click'); }, 200);
            }
          });

        })(jQuery);
