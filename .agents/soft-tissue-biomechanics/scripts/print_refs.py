import json
with open('D:\\Mello Silveira Serviços LTDA\\Projetos\\Tools\\.agents\\soft-tissue-biomechanics\\final_research.json', 'r', encoding='utf-8') as f:
    d = json.load(f)

for i, r in enumerate(d['data_payload']['references'][:50]):
    print(f"{i+1}. TITLE: {r['title']}")
    print(f"   DOI: {r['doi']}")
    print(f"   IMPACT: {r['citation_impact']}")
    print(f"   SUMMARY: {r['summary_of_read_content'][:100]}...")
