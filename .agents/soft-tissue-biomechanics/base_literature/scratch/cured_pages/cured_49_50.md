## [PAGE 49]

By Eqs. (6) and (7), we have
$$
dx_i = \frac{\partial x_i}{\partial a_j} da_j, \qquad da_i = \frac{\partial a_i}{\partial x_j} dx_j. \tag{10}
$$
Hence, on introducing the *Kronecker delta*, $\delta_{ij}$, which has the value 1 if $i = j$, and zero if $i \neq j$, we may write
$$
ds_0^2 = \delta_{ij} da_i da_j = \delta_{ij} \frac{\partial a_i}{\partial x_l} \frac{\partial a_j}{\partial x_m} dx_l dx_m, \tag{11}
$$
$$
ds^2 = \delta_{ij} dx_i dx_j = \delta_{ij} \frac{\partial x_i}{\partial a_l} \frac{\partial x_j}{\partial a_m} da_l da_m. \tag{12}
$$
The difference between the squares of the length elements may be written, after several changes in the symbols for dummy indices, either as
$$
ds^2 - ds_0^2 = \left( \delta_{\alpha\beta} \frac{\partial x_\alpha}{\partial a_i} \frac{\partial x_\beta}{\partial a_j} - \delta_{ij} \right) da_i da_j, \tag{13}
$$
or as
$$
ds^2 - ds_0^2 = \left( \delta_{ij} - \delta_{\alpha\beta} \frac{\partial a_\alpha}{\partial x_i} \frac{\partial a_\beta}{\partial x_j} \right) dx_i dx_j. \tag{14}
$$
We define the strain tensors
$$
E_{ij} = \frac{1}{2} \left( \delta_{\alpha\beta} \frac{\partial x_\alpha}{\partial a_i} \frac{\partial x_\beta}{\partial a_j} - \delta_{ij} \right), \tag{15}
$$
$$
e_{ij} = \frac{1}{2} \left( \delta_{ij} - \delta_{\alpha\beta} \frac{\partial a_\alpha}{\partial x_i} \frac{\partial a_\beta}{\partial x_j} \right), \tag{16}
$$
so that
$$
ds^2 - ds_0^2 = 2 E_{ij} da_i da_j, \tag{17}
$$
$$
ds^2 - ds_0^2 = 2 e_{ij} dx_i dx_j. \tag{18}
$$
The strain tensor $E_{ij}$ was introduced by Green and St.-Venant and is called *Green's strain tensor*. The strain tensor $e_{ij}$ was introduced by Cauchy for infinitesimal strains and by Almansi and Hamel for finite strains and is known as *Almansi's strain tensor*. In analogy with terminology in hydrodynamics, $E_{ij}$ is often referred to as *Lagrangian* and $e_{ij}$ as *Eulerian*.

$E_{ij}$ and $e_{ij}$ thus defined are tensors. They are symmetric; i.e.,
$$
E_{ij} = E_{ji}, \qquad e_{ij} = e_{ji}. \tag{19}
$$
An immediate consequence of Eqs. (17) and (18) is that $ds^2 - ds_0^2 = 0$ implies $E_{ij} = e_{ij} = 0$ and vice versa. But a deformation in which the length of every line element remains unchanged is a rigid-body motion. Hence, the necessary and sufficient condition that a deformation of a body be a rigid-

## [PAGE 50]

body motion is that all components of the strain tensor $E_{ij}$ or $e_{ij}$ be zero throughout the body.

If the components of displacement $u_i$ are such that their first derivatives are so small that the squares and products of the partial derivatives of $u_i$ are negligible compared with the first order terms, then $e_{ij}$ reduces to *Cauchy's infinitesimal strain tensor*,
$$
\varepsilon_{ij} = \frac{1}{2} \left( \frac{\partial u_j}{\partial x_i} + \frac{\partial u_i}{\partial x_j} \right). \tag{20}
$$
In unabridged notation, writing $u, v, w$ for $u_1, u_2, u_3$ and $x, y, z$ for $x_1, x_2, x_3$, we have

Figure 2.3:3 Deformation gradients and interpretation of infinitesimal strain components.
