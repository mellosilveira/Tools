import json
with open('D:\\Mello Silveira Serviços LTDA\\Projetos\\Tools\\.agents\\soft-tissue-biomechanics\\final_research.json', 'r', encoding='utf-8') as f:
    d = json.load(f)

for i, r in enumerate(d['data_payload']['references'][:50]):
    print(f"{r['doi']}@@{r['title'][:40]}")
