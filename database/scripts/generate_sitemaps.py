"""
Static Sitemap Generator for JP Reference
Uses streaming approach with server-side cursors
for high-performance sitemap generation.
"""
import os
import psycopg2
from datetime import datetime
import urllib.parse

def get_db_connection():
    return psycopg2.connect(
        host=os.environ.get('POSTGRES_HOST', 'postgres'),
        port=os.environ.get('POSTGRES_PORT', '5432'),
        database=os.environ.get('POSTGRES_DB', 'jlpt_reference'),
        user=os.environ.get('POSTGRES_USER', 'postgres'),
        password=os.environ.get('POSTGRES_PASSWORD', 'postgres')
    )

SITEMAP_HEADER = '<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n'
SITEMAP_FOOTER = '</urlset>\n'
INDEX_HEADER = '<?xml version="1.0" encoding="UTF-8"?>\n<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n'
INDEX_FOOTER = '</sitemapindex>\n'
MAX_URLS_PER_SITEMAP = 40000
CURSOR_FETCH_SIZE = 5000  # Fetch 5000 rows at a time instead of default

def write_url(f, url):
    """Write a single URL entry to the sitemap file."""
    f.write(f'<url><loc>{url}</loc><changefreq>monthly</changefreq><priority>0.8</priority></url>\n')

def stream_sitemap(conn, query, url_prefix, output_dir, base_url, sitemap_prefix):
    """
    Stream rows from DB directly to sitemap files using COPY for maximum speed.
    """
    sitemap_files = []
    file_index = 1
    url_count = 0
    current_file = None
    total_urls = 0
    
    # Use COPY for maximum throughput
    copy_query = f"COPY ({query}) TO STDOUT"
    cur = conn.cursor()
    
    # Create a pipe to read from COPY
    with cur.copy(copy_query) as copy:
        for row in copy.rows():
            # Start new file if needed
            if current_file is None or url_count >= MAX_URLS_PER_SITEMAP:
                if current_file:
                    current_file.write(SITEMAP_FOOTER)
                    current_file.close()
                    print(f"    Wrote {url_count} URLs to sitemap-{sitemap_prefix}-{file_index - 1}.xml")
                
                filename = f'sitemap-{sitemap_prefix}-{file_index}.xml'
                sitemap_files.append(filename)
                current_file = open(os.path.join(output_dir, filename), 'w', encoding='utf-8')
                current_file.write(SITEMAP_HEADER)
                file_index += 1
                url_count = 0
            
            # Write URL
            term = row[0]
            encoded_term = urllib.parse.quote(term, safe='()')
            full_url = f"{base_url.rstrip('/')}{url_prefix}{encoded_term}"
            write_url(current_file, full_url)
            url_count += 1
            total_urls += 1
    
    # Close last file
    if current_file:
        current_file.write(SITEMAP_FOOTER)
        current_file.close()
        if url_count > 0:
            print(f"    Wrote {url_count} URLs to sitemap-{sitemap_prefix}-{file_index - 1}.xml")
    
    cur.close()
    print(f"    Total: {total_urls} URLs")
    return sitemap_files

def stream_sitemap_cursor(conn, query, url_prefix, output_dir, base_url, sitemap_prefix):
    """
    Fallback: Stream using server-side cursor with increased fetch size.
    """
    sitemap_files = []
    file_index = 1
    url_count = 0
    current_file = None
    total_urls = 0
    
    # Use server-side cursor with large fetch size
    cursor_name = f"{sitemap_prefix}_cursor"
    cur = conn.cursor(name=cursor_name)
    cur.itersize = CURSOR_FETCH_SIZE
    cur.execute(query)
    
    for row in cur:
        # Start new file if needed
        if current_file is None or url_count >= MAX_URLS_PER_SITEMAP:
            if current_file:
                current_file.write(SITEMAP_FOOTER)
                current_file.close()
                print(f"    Wrote {url_count} URLs to sitemap-{sitemap_prefix}-{file_index - 1}.xml")
            
            filename = f'sitemap-{sitemap_prefix}-{file_index}.xml'
            sitemap_files.append(filename)
            current_file = open(os.path.join(output_dir, filename), 'w', encoding='utf-8')
            current_file.write(SITEMAP_HEADER)
            file_index += 1
            url_count = 0
        
        # Write URL
        term = row[0]
        encoded_term = urllib.parse.quote(term, safe='()')
        full_url = f"{base_url.rstrip('/')}{url_prefix}{encoded_term}"
        write_url(current_file, full_url)
        url_count += 1
        total_urls += 1
    
    # Close last file
    if current_file:
        current_file.write(SITEMAP_FOOTER)
        current_file.close()
        if url_count > 0:
            print(f"    Wrote {url_count} URLs to sitemap-{sitemap_prefix}-{file_index - 1}.xml")
    
    cur.close()
    print(f"    Total: {total_urls} URLs")
    return sitemap_files

def generate_static_sitemap(output_dir, base_url, static_urls):
    """Generate sitemap for static pages."""
    filename = 'sitemap-static-1.xml'
    filepath = os.path.join(output_dir, filename)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(SITEMAP_HEADER)
        for url in static_urls:
            full_url = f"{base_url.rstrip('/')}{url}"
            write_url(f, full_url)
        f.write(SITEMAP_FOOTER)
    
    return [filename]

def generate_sitemap_index(output_dir, base_url, all_sitemap_files):
    """Generate the main sitemap index file."""
    filepath = os.path.join(output_dir, 'sitemap.xml')
    lastmod = datetime.now().strftime('%Y-%m-%d')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(INDEX_HEADER)
        for sm in all_sitemap_files:
            f.write(f'<sitemap><loc>{base_url.rstrip("/")}/sitemap/{sm}</loc><lastmod>{lastmod}</lastmod></sitemap>\n')
        f.write(INDEX_FOOTER)

def generate_robots_txt(output_dir, base_url):
    """Generate robots.txt file with sitemap reference."""
    filepath = os.path.join(output_dir, 'robots.txt')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write('User-agent: *\n')
        f.write('Allow: /\n')
        f.write('\n')
        f.write(f'Sitemap: {base_url.rstrip("/")}/sitemap/sitemap.xml\n')
    
    print(f"Generated robots.txt")

def generate_sitemaps():
    base_url = os.environ.get('FRONTEND_URL', 'http://localhost:3000')
    output_dir = '/app/sitemap'
    os.makedirs(output_dir, exist_ok=True)
    
    conn = get_db_connection()
    all_sitemap_files = []
    
    print("Generating static pages sitemap...")
    static_files = generate_static_sitemap(output_dir, base_url, ['/', '/about', '/search'])
    all_sitemap_files.extend(static_files)
    print(f"  Created {len(static_files)} file(s)")
    
    print("Generating kanji sitemap...")
    kanji_files = stream_sitemap_cursor(
        conn,
        "SELECT literal FROM jlpt.kanji ORDER BY frequency ASC NULLS LAST",
        "/kanji/",
        output_dir, base_url, "kanji"
    )
    all_sitemap_files.extend(kanji_files)
    print(f"  Created {len(kanji_files)} file(s)")
    
    print("Generating radical sitemap...")
    radical_files = stream_sitemap_cursor(
        conn,
        "SELECT DISTINCT literal FROM jlpt.radical_group_member",
        "/radical/",
        output_dir, base_url, "radicals"
    )
    all_sitemap_files.extend(radical_files)
    print(f"  Created {len(radical_files)} file(s)")
    
    # proper noun query - generates unique URLs in format: term(reading)
    # This handles cases where same kanji has different readings (e.g., 日向平 can be ひがたびら, ひなたひら, etc.)
    print("Generating proper noun sitemap...")
    proper_noun_query = """
        SELECT pk.text || '(' || pka.text || ')' AS slug
        FROM jlpt.proper_noun_kanji pk
        JOIN jlpt.proper_noun_kana pka ON pk.proper_noun_id = pka.proper_noun_id AND pka.is_primary = true
        WHERE pk.is_primary = true
        UNION ALL
        SELECT pka.text AS slug
        FROM jlpt.proper_noun_kana pka
        LEFT JOIN jlpt.proper_noun_kanji pk ON pk.proper_noun_id = pka.proper_noun_id AND pk.is_primary = true
        WHERE pka.is_primary = true AND pk.proper_noun_id IS NULL
    """
    proper_noun_files = stream_sitemap_cursor(
        conn,
        proper_noun_query,
        "/proper-noun/",
        output_dir, base_url, "proper-noun"
    )
    all_sitemap_files.extend(proper_noun_files)
    print(f"  Created {len(proper_noun_files)} file(s)")
    
    # vocabulary query - generates unique URLs in format: term(reading)
    # This handles cases where same kanji has different readings
    print("Generating vocabulary sitemap...")
    vocab_query = """
        SELECT vk.text || '(' || vka.text || ')' AS slug
        FROM jlpt.vocabulary_kanji vk
        JOIN jlpt.vocabulary_kana vka ON vk.vocabulary_id = vka.vocabulary_id AND vka.is_primary = true
        WHERE vk.is_primary = true
        UNION ALL
        SELECT vka.text AS slug
        FROM jlpt.vocabulary_kana vka
        LEFT JOIN jlpt.vocabulary_kanji vk ON vk.vocabulary_id = vka.vocabulary_id AND vk.is_primary = true
        WHERE vka.is_primary = true AND vk.vocabulary_id IS NULL
    """
    vocab_files = stream_sitemap_cursor(
        conn,
        vocab_query,
        "/vocabulary/",
        output_dir, base_url, "vocabulary"
    )
    all_sitemap_files.extend(vocab_files)
    print(f"  Created {len(vocab_files)} file(s)")
    
    print("Generating sitemap index...")
    generate_sitemap_index(output_dir, base_url, all_sitemap_files)
    
    print("Generating robots.txt...")
    generate_robots_txt(output_dir, base_url)
    
    conn.close()
    print(f"Done! Generated {len(all_sitemap_files) + 1} sitemap files and robots.txt in {output_dir}")

if __name__ == "__main__":
    generate_sitemaps()
