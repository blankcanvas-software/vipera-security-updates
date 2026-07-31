-- Register Version 2.9.0 in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '2.9.0',
    'Vipera Security v2.9.0 - Enhanced Cyber Threat Intelligence, System Tray Protection & Automatic Security Updates',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
