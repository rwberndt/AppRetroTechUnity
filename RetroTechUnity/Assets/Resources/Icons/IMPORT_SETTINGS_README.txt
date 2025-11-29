═══════════════════════════════════════════════════════════════════════════
HIGH-QUALITY ICON IMPORT SETTINGS FOR UNITY UI
═══════════════════════════════════════════════════════════════════════════

Para obter a melhor qualidade visual dos ícones na navbar, configure as
seguintes opções no Inspector do Unity para cada arquivo .png:

📋 CONFIGURAÇÕES RECOMENDADAS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Texture Type: Sprite (2D and UI)
2. Sprite Mode: Single
3. Pixels Per Unit: 100

4. Filter Mode: Bilinear
   (ou Trilinear para qualidade ainda maior)

5. Compression: None
   ⚠️ IMPORTANTE: Desative a compressão para UI sprites!

6. Max Size: 2048 ou 4096
   (use 4096 para ícones de altíssima qualidade)

7. Format: RGBA 32 bit
   (garante qualidade máxima com transparência)

8. Generate Mip Maps: DESATIVADO
   (não é necessário para UI)

9. sRGB (Color Texture): ATIVADO

10. Alpha Is Transparency: ATIVADO

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎨 CONFIGURAÇÕES ESPECÍFICAS POR PLATAFORMA:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Para Android:
  - Override for Android: ✓ ATIVADO
  - Max Size: 2048
  - Format: RGBA 32 bit (ou ETC2 se precisar comprimir)
  - Compression Quality: 100 (Best)

Para iOS:
  - Override for iOS: ✓ ATIVADO
  - Max Size: 2048
  - Format: RGBA 32 bit (ou PVRTC se precisar comprimir)
  - Compression Quality: 100 (Best)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 DICAS ADICIONAIS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• Os ícones procedurais (gerados pelo HighQualityIconFactory.cs) são
  atualmente utilizados por padrão e oferecem qualidade superior

• Se quiser usar PNG assets:
  - Defina useProceduralIcons = false em GameManager.cs linha 393
  - Garanta que seus PNGs tenham resolução mínima de 512x512 pixels
  - Use anti-aliasing nas bordas dos ícones
  - Exporte com fundo transparente

• Tamanho ideal dos PNGs originais: 512x512 ou 1024x1024 pixels
  (muito maior que os 64px exibidos para garantir nitidez em telas Retina)

• Para ícones vetoriais/SVG: converta para PNG em alta resolução antes
  de importar no Unity

═══════════════════════════════════════════════════════════════════════════

🚀 SOLUÇÃO ATUAL (Ícones Procedurais):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

O sistema atual usa ícones gerados proceduralmente com:
✓ Resolução base de 256x256 pixels
✓ Anti-aliasing suave nas bordas
✓ Renderização vetorial (escalável sem perda de qualidade)
✓ Sem compressão ou artefatos
✓ Tamanho de arquivo mínimo (gerado em runtime)
✓ Totalmente personalizável via código

Veja: HighQualityIconFactory.cs

═══════════════════════════════════════════════════════════════════════════
