# Vocabulários controlados de Client

Versão do catálogo: **2026-11-21**. O contrato mantém os campos de persistência e transporte como strings opcionais. Não há endpoint novo nem alteração de esquema.

## Nacionalidade

Critério explícito: códigos ISO 3166-1 alpha-2 dos países das regiões estatísticas ONU M49 Europe (150) e Americas (019), limitados aos membros da ONU e à Santa Sé. São **79 entradas: 44 Europa e 35 Américas**. Os labels são nomes de países, não gentílicos, e usam `Client.Nationality.XX` nos quatro `common.json` (`pt-PT`, `pt-BR`, `en-US`, `es-ES`). `Region` tem os valores estáveis `Europe` e `Americas`.

A lista não equivale a uma definição universal de cidadania. Exclui territórios e dependências, mesmo quando têm código ISO; `XK` não é um código oficialmente atribuído ISO alpha-2 e está excluído. Chipre (`CY`), Turquia (`TR`), Arménia (`AM`), Azerbaijão (`AZ`) e Geórgia (`GE`) pertencem à Ásia ocidental na classificação escolhida; Cazaquistão (`KZ`) pertence à Ásia central. Rússia (`RU`) integra a Europa na M49. A Santa Sé (`VA`) é a exceção explícita de Estado observador incluída na Europa.

**Europa (44):** AD, AL, AT, BA, BE, BG, BY, CH, CZ, DE, DK, EE, ES, FI, FR, GB, GR, HR, HU, IE, IS, IT, LI, LT, LU, LV, MC, MD, ME, MK, MT, NL, NO, PL, PT, RO, RS, RU, SE, SI, SK, SM, UA, VA.

**Américas (35):** AG, AR, BB, BO, BR, BS, BZ, CA, CL, CO, CR, CU, DM, DO, EC, GD, GT, GY, HN, HT, JM, KN, LC, MX, NI, PA, PE, PY, SR, SV, TT, US, UY, VC, VE.

`IsCanonical(null)` é verdadeiro porque o campo é opcional. Strings vazias, espaços, minúsculas, nomes, gentílicos e códigos fora da lista são inválidos como valores novos. Não existe `OTHER`, `UNKNOWN`, `XX` ou outro sentinela arbitrário. Uma cidadania fora da cobertura não deve ser convertida num país incorreto: usar `null` quando o campo puder ficar sem preenchimento; ampliar o catálogo exige decisão de produto e nova versão.

### Países incluídos

| Código | Nome (pt-PT) | Região |
|---|---|---|
| AD | Andorra | Europa |
| AG | Antígua e Barbuda | Américas |
| AL | Albânia | Europa |
| AR | Argentina | Américas |
| AT | Áustria | Europa |
| BA | Bósnia e Herzegovina | Europa |
| BB | Barbados | Américas |
| BE | Bélgica | Europa |
| BG | Bulgária | Europa |
| BO | Bolívia | Américas |
| BR | Brasil | Américas |
| BS | Baamas | Américas |
| BY | Bielorrússia | Europa |
| BZ | Belize | Américas |
| CA | Canadá | Américas |
| CH | Suíça | Europa |
| CL | Chile | Américas |
| CO | Colômbia | Américas |
| CR | Costa Rica | Américas |
| CU | Cuba | Américas |
| CZ | Chéquia | Europa |
| DE | Alemanha | Europa |
| DK | Dinamarca | Europa |
| DM | Domínica | Américas |
| DO | República Dominicana | Américas |
| EC | Equador | Américas |
| EE | Estónia | Europa |
| ES | Espanha | Europa |
| FI | Finlândia | Europa |
| FR | França | Europa |
| GB | Reino Unido | Europa |
| GD | Granada | Américas |
| GR | Grécia | Europa |
| GT | Guatemala | Américas |
| GY | Guiana | Américas |
| HN | Honduras | Américas |
| HR | Croácia | Europa |
| HT | Haiti | Américas |
| HU | Hungria | Europa |
| IE | Irlanda | Europa |
| IS | Islândia | Europa |
| IT | Itália | Europa |
| JM | Jamaica | Américas |
| KN | São Cristóvão e Neves | Américas |
| LC | Santa Lúcia | Américas |
| LI | Listenstaine | Europa |
| LT | Lituânia | Europa |
| LU | Luxemburgo | Europa |
| LV | Letónia | Europa |
| MC | Mónaco | Europa |
| MD | Moldávia | Europa |
| ME | Montenegro | Europa |
| MK | Macedónia do Norte | Europa |
| MT | Malta | Europa |
| MX | México | Américas |
| NI | Nicarágua | Américas |
| NL | Países Baixos | Europa |
| NO | Noruega | Europa |
| PA | Panamá | Américas |
| PE | Peru | Américas |
| PL | Polónia | Europa |
| PT | Portugal | Europa |
| PY | Paraguai | Américas |
| RO | Roménia | Europa |
| RS | Sérvia | Europa |
| RU | Rússia | Europa |
| SE | Suécia | Europa |
| SI | Eslovénia | Europa |
| SK | Eslováquia | Europa |
| SM | São Marinho | Europa |
| SR | Suriname | Américas |
| SV | El Salvador | Américas |
| TT | Trindade e Tobago | Américas |
| UA | Ucrânia | Europa |
| US | Estados Unidos | Américas |
| UY | Uruguai | Américas |
| VA | Santa Sé | Europa |
| VC | São Vicente e Granadinas | Américas |
| VE | Venezuela | Américas |

## Género

Valores canónicos: `male`, `female`, `non-binary`, `prefer-not-to-say`; ausência é `null`. Labels localizados em `Client.Gender.<valor>`. A seleção é opcional e não deve inferir ou converter valores históricos automaticamente.

## Compatibilidade histórica

As leituras mantêm o valor armazenado. Não há atualização SQL nem migração de dados. Na edição, a regra do contrato preserva valores históricos quando o campo é omitido ou reenviado exatamente como armazenado; um valor diferente deve ser canónico ou `null` para limpar o campo. Espaços ou diferenças de capitalização não são equivalência canónica. A manutenção da lista e das traduções é explícita, não depende do ambiente, de `RegionInfo` nem de chamadas externas em runtime.

## Fontes e manutenção

- [ISO 3166 — country codes](https://www.iso.org/iso-3166-country-codes.html): formato alpha-2.
- [ONU M49 — tabela de regiões e códigos](https://unstats.un.org/unsd/methodology/m49/overview/): composição regional e códigos, consultados em 21/11/2026.
- [ONU — membros](https://www.un.org/en/about-us/member-states) e [Estados observadores](https://www.un.org/en/about-us/non-member-states): critério de inclusão.

A seleção de 79 entradas é uma política versionada do produto baseada nessas fontes; não é toda a lista ISO/M49. Mudanças devem rever códigos, regiões, os quatro labels e testes conjuntamente, sem alterar silenciosamente a interpretação dos dados históricos.
