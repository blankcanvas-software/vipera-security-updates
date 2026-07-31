-- Register Version 3.0.0 Major Milestone Release in Supabase app_versions table
INSERT INTO public.app_versions (version, release_notes, download_url, is_mandatory)
VALUES (
    '3.0.0',
    'Vipera Security 3.0.0 Major Release - Next-Gen Real-Time Cyber Protection Engine, System Tray Integration, Automated Hourly Scanning & Cloud AI Shield',
    'https://raw.githubusercontent.com/blankcanvas-software/vipera-security-updates/main/ViperaPayload.zip',
    false
)
ON CONFLICT (version) DO UPDATE 
SET release_notes = EXCLUDED.release_notes,
    download_url = EXCLUDED.download_url;
