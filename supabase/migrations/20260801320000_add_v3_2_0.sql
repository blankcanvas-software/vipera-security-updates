-- Register Version 3.2.0 Release in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '3.2.0',
    'Vipera Security 3.2.0 - Next-Gen Real-Time Cyber Protection, Background Scanning & Automatic Server Updates',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
