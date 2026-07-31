-- Register Version 3.1.0 Release in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '3.1.0',
    'Vipera Security 3.1.0 - Enhanced Cyber Threat Protection, System Tray Real-Time Shield & Server Update Engine',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
