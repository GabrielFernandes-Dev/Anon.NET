document.addEventListener('DOMContentLoaded', function () {
    // Elementos UI
    const queryTableBody = document.getElementById('query-table-body');
    const totalQueriesEl = document.getElementById('total-queries');
    const injectionCountEl = document.getElementById('injection-count');
    const avgDurationEl = document.getElementById('avg-duration');
    const queryTypeDistribution = document.getElementById('query-type-distribution');
    const refreshBtn = document.getElementById('refresh-btn');
    const severityFilter = document.getElementById('severity-filter');
    const queryTypeFilter = document.getElementById('query-type-filter');
    const modal = document.getElementById('query-detail-modal');
    const closeBtn = document.querySelector('.close');

    // Estado da aplicação
    let queries = [];
    let currentFilters = { severity: 'all', queryType: 'all' };

    // Inicialização
    fetchQueries();
    setupEventListeners();

    // Atualiza a cada 5 segundos
    setInterval(fetchQueries, 5000);

    function setupEventListeners() {
        refreshBtn.addEventListener('click', fetchQueries);
        severityFilter.addEventListener('change', applyFilters);
        queryTypeFilter.addEventListener('change', applyFilters);
        closeBtn.addEventListener('click', () => modal.style.display = 'none');

        // Fechar modal clicando fora
        window.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
        });
    }

    function fetchQueries() {
        fetch('api/queries')
            .then(response => {
                console.log(response)
                console.log(response.json())
                response.json()
            })
            .then(data => {
                console.log(data);
                queries = data;
                updateUI();
            })
            .catch(error => {
                console.error('Erro ao buscar queries:', error);
            });
    }

    function updateUI() {
        updateStats();
        updateQueryTable();
    }

    function updateStats() {
        // Total de queries
        totalQueriesEl.textContent = queries.length;

        // Total de possíveis injeções SQL
        const injectionCount = queries.filter(q =>
            q.additionalInfo && q.additionalInfo.SqlInjectionDetected === true
        ).length;
        injectionCountEl.textContent = injectionCount;

        // Tempo médio de execução
        const totalDuration = queries.reduce((sum, q) => sum + q.durationMs, 0);
        const avgDuration = queries.length > 0 ? (totalDuration / queries.length).toFixed(2) : 0;
        avgDurationEl.textContent = avgDuration;

        // Distribuição por tipo de query
        updateQueryTypeChart();
    }

    function updateQueryTypeChart() {
        // Contagem por tipo de query
        const typeCounts = {};
        queries.forEach(q => {
            const type = q.queryType || 'UNKNOWN';
            typeCounts[type] = (typeCounts[type] || 0) + 1;
        });

        // Limpar gráfico atual
        queryTypeDistribution.innerHTML = '';

        // Criar barras para cada tipo
        const maxCount = Math.max(...Object.values(typeCounts), 1);

        for (const [type, count] of Object.entries(typeCounts)) {
            const percentage = (count / maxCount) * 100;
            const bar = document.createElement('div');
            bar.className = 'chart-bar';
            bar.style.height = `${percentage}%`;

            const label = document.createElement('span');
            label.textContent = type;

            bar.appendChild(label);
            queryTypeDistribution.appendChild(bar);
        }
    }

    function applyFilters() {
        currentFilters.severity = severityFilter.value;
        currentFilters.queryType = queryTypeFilter.value;
        updateQueryTable();
    }

    function updateQueryTable() {
        queryTableBody.innerHTML = '';

        // Filtrar queries
        const filteredQueries = queries.filter(query => {
            // Filtro por severidade
            if (currentFilters.severity !== 'all') {
                const hasInjection = query.additionalInfo && query.additionalInfo.SqlInjectionDetected === true;

                if (currentFilters.severity === 'none' && hasInjection) return false;

                if (hasInjection && currentFilters.severity !== 'none') {
                    const severity = query.additionalInfo.SqlInjectionSeverity?.toLowerCase();
                    if (severity !== currentFilters.severity.toLowerCase()) return false;
                }

                if (currentFilters.severity !== 'none' && !hasInjection) return false;
            }

            // Filtro por tipo de query
            if (currentFilters.queryType !== 'all') {
                if (query.queryType !== currentFilters.queryType) return false;
            }

            return true;
        });

        // Renderizar linhas da tabela
        filteredQueries.forEach(query => {
            const row = document.createElement('tr');

            // Verificar se há detecção de SQL Injection
            let hasInjection = query.additionalInfo.SqlInjectionDetected;
            let severityClass = 'severity-none';
            let severityText = 'Nenhuma';

            if (hasInjection) {
                const severity = query.additionalInfo.SqlInjectionSeverity?.toLowerCase();
                if (severity === 'critical') {
                    severityClass = 'severity-high';
                    severityText = 'Critica';
                }
                else if (severity === 'high') {
                    severityClass = 'severity-high';
                    severityText = 'Alta';
                } else if (severity === 'medium') {
                    severityClass = 'severity-medium';
                    severityText = 'Média';
                } else if (severity === 'low') {
                    severityClass = 'severity-low';
                    severityText = 'Baixa';
                }
            }

            const timestamp = new Date(query.timestamp).toLocaleString();

            row.innerHTML = `
                        <td>${query.id.substring(0, 8)}...</td>
                        <td>${timestamp}</td>
                        <td>${query.queryType || 'UNKNOWN'}</td>
                        <td>${query.durationMs} ms</td>
                        <td>${hasInjection ? 'Sim' : 'Não'}</td>
                        <td class="${severityClass}">${severityText}</td>
                        <td><button class="action-btn btn btn-primary text-center" data-id="${query.id}">Detalhes</button></td>
                    `;

            const detailBtn = row.querySelector('.action-btn');
            detailBtn.addEventListener('click', () => showDetailModal(query));

            queryTableBody.appendChild(row);
        });
    }

    function showDetailModal(query) {
        console.log("Chamando Modal");
        console.log(query);
        // Preencher informações do modal
        document.getElementById('detail-id').textContent = query.id;
        document.getElementById('detail-timestamp').textContent = new Date(query.timestamp).toLocaleString();
        document.getElementById('detail-type').textContent = query.queryType || 'UNKNOWN';
        document.getElementById('detail-duration').textContent = query.durationMs;
        document.getElementById('detail-source').textContent = query.source || 'N/A';
        document.getElementById('detail-sql').textContent = query.commandText || 'N/A';
        document.getElementById('detail-parameters').textContent = JSON.stringify(query.parameters || {}, null, 2);

        // Seção de SQL Injection
        const injectionSection = document.getElementById('injection-section');
        const hasInjection = query.additionalInfo && query.additionalInfo.SqlInjectionDetected === true;

        if (hasInjection) {
            injectionSection.style.display = 'block';
            document.getElementById('detail-injection-detected').textContent = 'Sim';
            document.getElementById('detail-injection-pattern').textContent = query.additionalInfo.SqlInjectionPattern || 'N/A';

            let severityText = 'N/A';
            if (query.additionalInfo.SqlInjectionSeverity) {
                const severity = query.additionalInfo.SqlInjectionSeverity.toLowerCase();
                if (severity === 'high') severityText = 'Alta';
                else if (severity === 'medium') severityText = 'Média';
                else if (severity === 'low') severityText = 'Baixa';
            }

            document.getElementById('detail-injection-severity').textContent = severityText;
            document.getElementById('detail-injection-parameter').textContent = query.additionalInfo.sqlInjectionParameter || 'N/A';
        } else {
            injectionSection.style.display = 'none';
        }

        // Exibir modal
        modal.style.display = 'block';
    }
});

function hideDetailModal() {
    const modal = document.getElementById('query-detail-modal');
    modal.style.display = 'none';
}
