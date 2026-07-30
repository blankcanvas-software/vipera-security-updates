-- Register version 2.1.0 for live over-the-air server updates
INSERT INTO public.app_versions (version, release_notes, download_url) 
VALUES (
    '2.1.0', 
    'Vipera Security v2.1.0 - Startup Auto-Update & Hourly Background Scanner Engine', 
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip'
) ON CONFLICT (version) DO UPDATE SET download_url = EXCLUDED.download_url;
