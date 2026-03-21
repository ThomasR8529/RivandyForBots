const fs = require("fs");
const path = require("path");

const dir = "Assets/0-ETU/Scripts";

if (!fs.existsSync(dir)) {
  console.error("❌ Dossier introuvable :", dir);
  process.exit(1);
}

// 🔍 lire tous les fichiers .cs
const files = fs.readdirSync(dir).filter(f => f.endsWith(".cs"));

let content = `# Rivandy\n\n## 📁 Scripts Overview\n`;

files.forEach(file => {
  const filePath = path.join(dir, file);
  const code = fs.readFileSync(filePath, "utf-8");

  // Classe
  const className = code.match(/class\s+(\w+)/)?.[1] || file;

  // Commentaires XML
  const comments = [...code.matchAll(/\/\/\/\s?(.*)/g)]
    .map(m => m[1])
    .join(" ");

  // Méthodes
  const methods = [...code.matchAll(/(public|private)\s+\w+\s+(\w+)\s*\(/g)]
    .map(m => m[2]);

  content += `\n### 📜 ${className}\n`;
  content += `- File: \`${file}\`\n`;
  content += `- Methods:\n`;
  methods.forEach(m => {
    content += `  - ${m}\n`;
  });
});

fs.writeFileSync("README.md", content);