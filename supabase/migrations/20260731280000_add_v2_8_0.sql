-- Register Version 2.8.0 in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '2.8.0',
    'Vipera Security v2.8.0 - Enhanced System Performance, 24/7 Threat Protection & Server Update Engine',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
