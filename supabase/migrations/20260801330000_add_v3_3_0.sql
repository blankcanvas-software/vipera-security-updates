-- Register Version 3.3.0 Release in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '3.3.0',
    'Vipera Security 3.3.0 - Enhanced Threat Intelligence, Background Scanning & Automatic Server Update Engine',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
